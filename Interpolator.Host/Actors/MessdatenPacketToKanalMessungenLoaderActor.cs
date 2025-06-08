using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

public record LoadAdllKanalMessungenCommand(double InterpolationsOffset = 0.1, bool Force = false);

public class MessdatenPackeToKanalMessungenLoaderActor : CsvLoaderActorBase, IWithTimers
{
  public ITimerScheduler? Timers { get; set; }
  private readonly ILogger<MessdatenPackeToKanalMessungenLoaderActor> _logger;
  private readonly IDocumentStore _documentStore;

  public MessdatenPackeToKanalMessungenLoaderActor(
    ILogger<MessdatenPackeToKanalMessungenLoaderActor> logger,
    IDocumentStore documentStore
  )
  {
    _logger = logger;
    _documentStore = documentStore;
  }

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

        LoadAllKanalMessungenCommandHandler(loadAdllKanalMessungenCommand.InterpolationsOffset);
        break;
    }
  }

  private void LoadAllKanalMessungenCommandHandler(double interpolationsOffset)
  {
    _logger.LogInformation(
      "*** Kommando {Command} erhalten",
      nameof(LoadAdllKanalMessungenCommand)
    );
    using var lightweightSession = _documentStore.LightweightSession();

    _logger.LogInformation("*** Lösche alle existierende {Messung}...", nameof(KanalMessung));
    lightweightSession.DeleteWhere<KanalMessung>(x => true);
    lightweightSession.SaveChangesAsync().GetAwaiter().GetResult();

    _logger.LogInformation("*** Suche alle {MessdatenPaket}...", nameof(MessdatenPaket));
    var kanalMessungen = lightweightSession
      .Query<MessdatenPaket>()
      .Where(messdatenPaket =>
        messdatenPaket.MessdatenMimeType == "text/csv"
        && messdatenPaket.Messart == Messart.Anemometer
      )
      .AsEnumerable()
      .SelectMany(messdatenPaket =>
        MessdatenPaketToKanalMessung(messdatenPaket, interpolationsOffset)
      );

    foreach (var kanalMessung in kanalMessungen)
    {
      _logger.LogInformation("*** KanalMessung: '{KanalMessung}'", kanalMessung);
      lightweightSession.Store(kanalMessung);
    }

    lightweightSession.SaveChangesAsync().GetAwaiter().GetResult();

    _logger.LogInformation(
      "*** {Anzahl} {KanalMessung} erstellt & gespeichert!",
      kanalMessungen.Count(),
      nameof(KanalMessung)
    );
  }

  private List<KanalMessung> MessdatenPaketToKanalMessung(
    MessdatenPaket messdatenPaket,
    double interpolationsOffset
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
      using var streamReader = new StreamReader(new MemoryStream(messdatenPaket.Messdaten));
      using var csvReader = new CsvReader(streamReader, _csvReaderConfiguration);

      var anemometerMessungen = csvReader.GetRecords<AnemometerCsv>().ToList();

      var temperaturMesspunkte = new List<AngereicherterMesspunkt>();
      var luftfeuchtigkeitMesspunkte = new List<AngereicherterMesspunkt>();
      var windgeschwindigkeitMesspunkte = new List<AngereicherterMesspunkt>();

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

        _logger.LogInformation(
          "*** Verarbeite {AnemometerCsv} {Index}...",
          nameof(AnemometerCsv),
          anemomenterMessungIndex
        );

        TimeOnly uhrzeit = TimeOnly.Parse(anemometerMessung.Zeit, CultureInfo.InvariantCulture);
        DateTime zeitstempel = abschlussdatum.ToDateTime(uhrzeit);
        double xCoord = (zeitstempel - DateTime.UnixEpoch).TotalSeconds;

        double yCoordTemperaturInGradCelsius = GetDouble(anemometerMessung.TemperaturInGradCelsius);
        double yCoordLuftfeuchtigkeitInProzent = GetDouble(
          anemometerMessung.LuftfeuchtigkeitInProzent
        );
        double yCoordWindgeschwindigkeitInMeterProSekunde = GetDouble(
          anemometerMessung.WindgeschwindigkeitInMeterProSekunde
        );

        var temperaturMesspunkt = new AngereicherterMesspunkt(
          xCoord,
          yCoordTemperaturInGradCelsius
        );
        var luftfeuchtigkeitMesspunkt = new AngereicherterMesspunkt(
          xCoord,
          yCoordLuftfeuchtigkeitInProzent
        );
        var windgeschwindigkeitMesspunkt = new AngereicherterMesspunkt(
          xCoord,
          yCoordWindgeschwindigkeitInMeterProSekunde
        );

        temperaturMesspunkte.Add(temperaturMesspunkt);
        luftfeuchtigkeitMesspunkte.Add(luftfeuchtigkeitMesspunkt);
        windgeschwindigkeitMesspunkte.Add(windgeschwindigkeitMesspunkt);
      }

      Task.WhenAll(
          Task.Run(() =>
          {
            _logger.LogInformation("*** Berechne Spline für Temperatur-Messpunkte...");
            temperaturMesspunkte.CalculateSpline();
            _logger.LogInformation("*** Spline für Temperatur-Messpunkte berechnet!");
          }),
          Task.Run(() =>
          {
            _logger.LogInformation("*** Berechne Spline für Luftfeuchtigkeit-Messpunkte...");
            luftfeuchtigkeitMesspunkte.CalculateSpline();
            _logger.LogInformation("*** Spline für Luftfeuchtigkeit-Messpunkte berechnet!");
          }),
          Task.Run(() =>
          {
            _logger.LogInformation("*** Berechne Spline für Windgeschwindigkeit-Messpunkte...");
            windgeschwindigkeitMesspunkte.CalculateSpline();
            _logger.LogInformation("*** Spline für Windgeschwindigkeit-Messpunkte berechnet!");
          })
        )
        .GetAwaiter()
        .GetResult();
      _logger.LogInformation("*** Alle Splines berechnet! 👌");

      var nbrMessungen = anemometerMessungen.Count;
      var temperaturInterpolationspunkte = new List<Interpolationspunkt>();
      var luftfeuchtigkeitInterpolationspunkte = new List<Interpolationspunkt>();
      var windgeschwindigkeitInterpolationspunkte = new List<Interpolationspunkt>();

      for (int i = 0; i < nbrMessungen; i++)
      {
        _logger.LogInformation("*** Berechne Interpolationspunkte für Messung {Index}...", i);
        AngereicherterMesspunkt temperaturMesspunkt = temperaturMesspunkte[i];
        AngereicherterMesspunkt luftfeuchtigkeitMesspunkt = luftfeuchtigkeitMesspunkte[i];
        AngereicherterMesspunkt windgeschwindigkeitMesspunkt = windgeschwindigkeitMesspunkte[i];

        var nextIndex = i + 1;

        if (nextIndex < nbrMessungen)
        {
          double xCoordNext = temperaturMesspunkte[nextIndex].X;
          double xCoordCurrent = temperaturMesspunkte[i].X;

          while (xCoordCurrent < xCoordNext)
          {
            _logger.LogInformation(
              "*** Interpolationspunkt für Offset {Offset} wird berechnet...",
              xCoordCurrent
            );
            temperaturInterpolationspunkte.Add(
              new Interpolationspunkt(temperaturMesspunkt, xCoordCurrent)
            );
            luftfeuchtigkeitInterpolationspunkte.Add(
              new Interpolationspunkt(luftfeuchtigkeitMesspunkt, xCoordCurrent)
            );
            windgeschwindigkeitInterpolationspunkte.Add(
              new Interpolationspunkt(windgeschwindigkeitMesspunkt, xCoordCurrent)
            );

            xCoordCurrent += interpolationsOffset;
          }
        }
        else
        {
          temperaturInterpolationspunkte.Add(new Interpolationspunkt(temperaturMesspunkt, 0));
          luftfeuchtigkeitInterpolationspunkte.Add(
            new Interpolationspunkt(luftfeuchtigkeitMesspunkt, 0)
          );
          windgeschwindigkeitInterpolationspunkte.Add(
            new Interpolationspunkt(windgeschwindigkeitMesspunkt, 0)
          );
        }
        _logger.LogInformation(
          "*** Berechnung Interpolationspunkte für Messung {Index} abgeschlossen!",
          i
        );
      }

      for (int i = 0; i < temperaturInterpolationspunkte.Count; i++)
      {
        _logger.LogInformation("*** Berechne {KanalMessung} {Index}", nameof(KanalMessung), i);
        var currentTemperaturInterpolationspunkt = temperaturInterpolationspunkte[i];
        result.Add(
          new KanalMessung
          {
            Id = Guid.NewGuid(),
            TemperaturInGradCelsius = currentTemperaturInterpolationspunkt.Y,
            LuftfeuchtigkeitInProzent = luftfeuchtigkeitInterpolationspunkte[i].Y,
            WindgeschwindigkeitInMeterProSekunde = windgeschwindigkeitInterpolationspunkte[i].Y,
            Zeitstempel = DateTime.UnixEpoch.AddSeconds(currentTemperaturInterpolationspunkt.X),
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
