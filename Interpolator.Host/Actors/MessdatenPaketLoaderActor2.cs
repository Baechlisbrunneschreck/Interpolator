using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Akka.Actor;

using CsvHelper;
using CsvHelper.Configuration;

using Interpolator.Host.Models;
using Interpolator.Host.Models.Aggregates;
using Interpolator.Host.Models.Csv;

using Marten;

using Microsoft.Extensions.Logging;

namespace Interpolator.Host.Actors;

public record LoadAllPhotovoltaikMessungenCommand;

public class MessdatenPaketLoaderActor2 : UntypedActor, IWithTimers
{
  private readonly CsvConfiguration _csvReaderConfiguration = new(CultureInfo.InvariantCulture)
  {
    Delimiter = ";",
    HasHeaderRecord = true,
    IgnoreBlankLines = true,
    TrimOptions = TrimOptions.Trim,
  };

  private readonly IDocumentStore _documentStore;

  private readonly NumberFormatInfo _doubleFormatProvider = new() { NumberDecimalSeparator = "," };

  private readonly ILogger<MessdatenPaketLoaderActor2> _logger;

  public MessdatenPaketLoaderActor2(
    ILogger<MessdatenPaketLoaderActor2> logger,
    IDocumentStore documentStore
  )
  {
    _logger = logger;
    _documentStore = documentStore;
  }

  public ITimerScheduler? Timers { get; set; }

  public override void AroundPostStop()
  {
    _logger.LogInformation("*** MessdatenPaketLoaderActor stopped");
  }

  public override void AroundPreStart()
  {
    _logger.LogInformation("*** MessdatenPaketLoaderActor started");
    ScheduleNewLoadAllCsvMessdatenPaketCommand();
  }

  protected override void OnReceive(object message)
  {
    switch (message)
    {
      case LoadAllPhotovoltaikMessungenCommand:
        LoadAllPhotovoltaikMessungenCommandHandler();
        break;
    }
  }

  private double GetDouble(string? input)
  {
    return double.Parse(input ?? "0", NumberStyles.Any, _doubleFormatProvider);
  }

  private void LoadAllPhotovoltaikMessungenCommandHandler()
  {
    using var lightweightSession = _documentStore.LightweightSession();

    lightweightSession.DeleteWhere<PhotovoltaikMessung>(x => true);
    lightweightSession.SaveChangesAsync().GetAwaiter().GetResult();

    var photovoltaikMessungen = lightweightSession
      .Query<MessdatenPaket>()
      .Where(messdatenPaket =>
        messdatenPaket.MessdatenMimeType == "text/csv"
        && messdatenPaket.Messart == Messart.Stromleistung
      )
      .SelectMany(MessdatenPaketToPhotovoltaikMessung);

    foreach (var photovoltaikMessung in photovoltaikMessungen)
    {
      _logger.LogInformation(
        "*** PhotovoltaikMessung: '{PhotovoltaikMessung}'",
        photovoltaikMessung
      );
      lightweightSession.Store(photovoltaikMessung);
    }

    lightweightSession.SaveChangesAsync().GetAwaiter().GetResult();
  }

  private IEnumerable<PhotovoltaikMessung> MessdatenPaketToPhotovoltaikMessung(
    MessdatenPaket messdatenPaket
  )
  {
    List<PhotovoltaikMessung> result = new();
    DateOnly abschlussdatum = DateOnly.FromDateTime(messdatenPaket.Abschlusszeitpunkt);

    if (messdatenPaket.Messdaten != null)
    {
      using var streamReader = new StreamReader(new MemoryStream(messdatenPaket.Messdaten));
      using var csvReader = new CsvReader(streamReader, _csvReaderConfiguration);

      foreach (var stromleistungMessung in csvReader.GetRecords<StromleistungCsv>())
      {
        var leistungInWatt = GetDouble(stromleistungMessung.LeistungInWatt);
        var uhrzeit = TimeOnly.Parse(stromleistungMessung.Uhrzeit);

        result.Add(
          new PhotovoltaikMessung
          {
            Id = Guid.NewGuid(),
            Zeitstempel = abschlussdatum.ToDateTime(uhrzeit),
            LeistungInWatt = leistungInWatt,
          }
        );
      }
    }

    return result;
  }

  private void ScheduleNewLoadAllCsvMessdatenPaketCommand()
  {
    Timers?.StartSingleTimer(
      nameof(LoadAllPhotovoltaikMessungenCommand),
      new LoadAllPhotovoltaikMessungenCommand(),
      TimeSpan.FromSeconds(15)
    );
  }
}