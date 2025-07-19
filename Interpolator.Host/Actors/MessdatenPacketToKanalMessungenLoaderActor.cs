using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Akka.Actor;
using CsvHelper;
using Interpolator.Domain.Extensions;
using Interpolator.Domain.Models;
using Interpolator.Host.Actors.Bases;
using Interpolator.Host.Models;
using Interpolator.Host.Models.Aggregates;
using Interpolator.Host.Models.Csv;
using Marten;
using Microsoft.Extensions.Logging;

namespace Interpolator.Host.Actors;

public record LoadAdllKanalMessungenCommand(
  double GlobaleGewichtung,
  double InterpolationsOffset = 0.1,
  bool Force = false
);

public class MessdatenPackeToKanalMessungenLoaderActor : CsvLoaderActorBase, IWithTimers
{
  private readonly IDocumentStore _documentStore;
  private readonly ILogger<MessdatenPackeToKanalMessungenLoaderActor> _logger;

  public MessdatenPackeToKanalMessungenLoaderActor(
    ILogger<MessdatenPackeToKanalMessungenLoaderActor> logger,
    IDocumentStore documentStore
  )
  {
    _logger = logger;
    _documentStore = documentStore;
  }

  public ITimerScheduler? Timers { get; set; }

  protected override void OnReceive(object message)
  {
    switch (message)
    {
      case LoadAdllKanalMessungenCommand loadAdllKanalMessungenCommand:
        if (loadAdllKanalMessungenCommand.InterpolationsOffset <= 0)
        {
          Sender.Tell("InterpolationsOffset <= 0 ist nicht erlaubt!");
          break;
        }

        if (
          loadAdllKanalMessungenCommand.InterpolationsOffset > 100
          && !loadAdllKanalMessungenCommand.Force
        )
        {
          Sender.Tell(
            "InterpolationsOffset > 100 ist normalerweise nicht erlaubt. Dies kann auf Wunsch jedoch forciert werden."
          );
          break;
        }

        LoadAllKanalMessungenCommandHandler(
          loadAdllKanalMessungenCommand.InterpolationsOffset,
          loadAdllKanalMessungenCommand.GlobaleGewichtung
        );
        break;
    }
  }

  private void LoadAllKanalMessungenCommandHandler(
    double interpolationsOffset,
    double globaleGewichtung
  )
  {
    _logger.LogInformation(
      "*** Kommando {Command} erhalten",
      nameof(LoadAdllKanalMessungenCommand)
    );
    using IDocumentSession lightweightSession = _documentStore.LightweightSession();

    _logger.LogInformation("*** Lösche alle existierende {Messung}...", nameof(KanalMessung));
    lightweightSession.DeleteWhere<KanalMessung>(x => true);
    lightweightSession.SaveChangesAsync().GetAwaiter().GetResult();

    _logger.LogInformation("*** Suche alle {MessdatenPaket}...", nameof(MessdatenPaket));
    KanalMessung[] kanalMessungen = lightweightSession
      .Query<MessdatenPaket>()
      .Where(messdatenPaket =>
        messdatenPaket.MessdatenMimeType == "text/csv"
        && messdatenPaket.Messart == Messart.Anemometer
      )
      .AsEnumerable()
      .SelectMany(messdatenPaket =>
        MessdatenPaketToKanalMessung(messdatenPaket, interpolationsOffset, globaleGewichtung)
      )
      .ToArray();

    _documentStore.BulkInsert(kanalMessungen);

    lightweightSession.SaveChangesAsync().GetAwaiter().GetResult();

    _logger.LogInformation(
      "*** {Anzahl} {KanalMessung} erstellt & gespeichert!",
      kanalMessungen.Count(),
      nameof(KanalMessung)
    );
  }

  private List<KanalMessung> MessdatenPaketToKanalMessung(
    MessdatenPaket messdatenPaket,
    double interpolationsOffset,
    double globaleGewichtung
  )
  {
    List<KanalMessung> result = [];
    DateOnly abschlussdatum = DateOnly.FromDateTime(messdatenPaket.Abschlusszeitpunkt);

    if (messdatenPaket.Messdaten != null)
    {
      _logger.LogInformation(
        "*** {MessdatenPaket} mit Messdaten gefunden...",
        nameof(MessdatenPaket)
      );
      using StreamReader streamReader = new StreamReader(
        new MemoryStream(messdatenPaket.Messdaten)
      );
      using CsvReader csvReader = new CsvReader(streamReader, _csvReaderConfiguration);

      List<AnemometerCsv> anemometerMessungen = csvReader
        .GetRecords<AnemometerCsv>()
        .GroupBy(anemometerMessung => anemometerMessung.Zeit)
        .Select(grouping =>
        {
          if (grouping.Count() > 1)
          {
            double luftfeuchtigkeitInProzent = grouping.Average(anemomenterCsv =>
              GetDouble(anemomenterCsv.LuftfeuchtigkeitInProzent)
            );
            double temperaturInGradCelsius = grouping.Average(anemomenterCsv =>
              GetDouble(anemomenterCsv.TemperaturInGradCelsius)
            );
            double windgeschwindigkeitInMeterProSekunde = grouping.Average(anemomenterCsv =>
              GetDouble(anemomenterCsv.WindgeschwindigkeitInMeterProSekunde)
            );

            return new AnemometerCsv
            {
              Zeit = grouping.Key,
              LuftfeuchtigkeitInProzent = luftfeuchtigkeitInProzent.ToString(
                CultureInfo.InvariantCulture
              ),
              TemperaturInGradCelsius = temperaturInGradCelsius.ToString(
                CultureInfo.InvariantCulture
              ),
              WindgeschwindigkeitInMeterProSekunde = windgeschwindigkeitInMeterProSekunde.ToString(
                CultureInfo.InvariantCulture
              ),
            };
          }
          else
          {
            return grouping.First();
          }
        })
        .OrderBy(anemometerCsv => anemometerCsv.Zeit)
        .ToList();

      List<Messpunkt> temperaturMesspunkte = new List<Messpunkt>();
      List<Messpunkt> luftfeuchtigkeitMesspunkte = new List<Messpunkt>();
      List<Messpunkt> windgeschwindigkeitMesspunkte = new List<Messpunkt>();

      for (
        int anemomenterMessungIndex = 0;
        anemomenterMessungIndex < anemometerMessungen.Count;
        anemomenterMessungIndex++
      )
      {
        AnemometerCsv anemometerMessung = anemometerMessungen[anemomenterMessungIndex];

        if (string.IsNullOrWhiteSpace(anemometerMessung.Zeit))
        {
          _logger.LogError(
            "*** Messung @ Index '{Index}' enthält keine gültige Zeitangabe: '{Zeit}'",
            anemomenterMessungIndex,
            anemometerMessung.Zeit
          );

          continue;
        }

        TimeOnly uhrzeit = TimeOnly.Parse(anemometerMessung.Zeit, CultureInfo.InvariantCulture);
        DateTime zeitstempel = abschlussdatum.ToDateTime(uhrzeit);
        double xCoord = anemomenterMessungIndex;

        double yCoordTemperaturInGradCelsius = GetDouble(anemometerMessung.TemperaturInGradCelsius);
        double yCoordLuftfeuchtigkeitInProzent = GetDouble(
          anemometerMessung.LuftfeuchtigkeitInProzent
        );
        double yCoordWindgeschwindigkeitInMeterProSekunde = GetDouble(
          anemometerMessung.WindgeschwindigkeitInMeterProSekunde
        );

        Messpunkt temperaturMesspunkt = new Messpunkt(
          xCoord,
          yCoordTemperaturInGradCelsius,
          zeitstempel
        );
        Messpunkt luftfeuchtigkeitMesspunkt = new Messpunkt(
          xCoord,
          yCoordLuftfeuchtigkeitInProzent,
          zeitstempel
        );
        Messpunkt windgeschwindigkeitMesspunkt = new Messpunkt(
          xCoord,
          yCoordWindgeschwindigkeitInMeterProSekunde,
          zeitstempel
        );

        temperaturMesspunkte.Add(temperaturMesspunkt);
        luftfeuchtigkeitMesspunkte.Add(luftfeuchtigkeitMesspunkt);
        windgeschwindigkeitMesspunkte.Add(windgeschwindigkeitMesspunkt);
      }

      List<Splinepunkt> temperaturInterpolationspunkte = temperaturMesspunkte
        .ToSplineMesspunkte(globaleGewichtung)
        .ToSplinepunkte(interpolationsOffset)
        .ToList();
      List<Splinepunkt> luftfeuchtigkeitInterpolationspunkte = luftfeuchtigkeitMesspunkte
        .ToSplineMesspunkte(globaleGewichtung)
        .ToSplinepunkte(interpolationsOffset)
        .ToList();
      List<Splinepunkt> windgeschwindigkeitInterpolationspunkte = windgeschwindigkeitMesspunkte
        .ToSplineMesspunkte(globaleGewichtung)
        .ToSplinepunkte(interpolationsOffset)
        .ToList();

      _logger.LogInformation("*** Alle Splines berechnet! 👌");

      for (int i = 0; i < temperaturInterpolationspunkte.Count; i++)
      {
        Splinepunkt currentTemperaturInterpolationspunkt = temperaturInterpolationspunkte[i];
        result.Add(
          new KanalMessung
          {
            Id = Guid.NewGuid(),
            TemperaturInGradCelsius = currentTemperaturInterpolationspunkt.Y,
            LuftfeuchtigkeitInProzent = luftfeuchtigkeitInterpolationspunkte[i].Y,
            WindgeschwindigkeitInMeterProSekunde = windgeschwindigkeitInterpolationspunkte[i].Y,
            Zeitstempel = currentTemperaturInterpolationspunkt.T,
          }
        );
      }
    }

    _logger.LogInformation(
      "*** Berechnung von {Anzahl} Kanalmessungen abgeschlossen 💪💪💪!",
      result.Count
    );

    return result;
  }
}
