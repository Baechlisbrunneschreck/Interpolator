using System;
using System.Globalization;
using System.IO;
using System.Linq;

using Akka.Actor;

using CsvHelper;
using CsvHelper.Configuration;

using Interpolator.Host.Models;
using Interpolator.Host.Models.Csv;

using Marten;

using Microsoft.Extensions.Logging;

namespace Interpolator.Host.Actors;

public record LoadAllCsvMessdatenPaketeCommand;

public class MessdatenPaketLoaderActor : UntypedActor, IWithTimers
{
  private readonly IDocumentStore _documentStore;
  private readonly ILogger<MessdatenPaketLoaderActor> _logger;
  private IQuerySession? _querySession;

  public MessdatenPaketLoaderActor(
    ILogger<MessdatenPaketLoaderActor> logger,
    IDocumentStore documentStore
  )
  {
    _logger = logger;
    _documentStore = documentStore;
  }

  public ITimerScheduler Timers { get; set; }

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
      case LoadAllCsvMessdatenPaketeCommand:
        LoadAllCsvMessdatenPaketeCommandHandler();
        break;
    }
  }

  private void LoadAllCsvMessdatenPaketeCommandHandler()
  {
    _logger.LogInformation("*** Loading all CSV MessdatenPakete...");
    _querySession = _documentStore.QuerySession();

    var csvReaderConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
      Delimiter = ";",
      HasHeaderRecord = true,
      IgnoreBlankLines = true,
      TrimOptions = TrimOptions.Trim,
    };

    NumberFormatInfo doubleFormatProvider = new() { NumberDecimalSeparator = "," };

    foreach (
      var messdatenPaket in _querySession
        .Query<MessdatenPaket>()
        .Where(p => p.MessdatenMimeType == "text/csv")
        .ToList()
    )
    {
      _logger.LogInformation("*** Found CSV MessdatenPaket with Id: {Messnummer}", messdatenPaket.Messnummer);

      if (messdatenPaket.Messdaten != null)
      {
        using var streamReader = new StreamReader(new MemoryStream(messdatenPaket.Messdaten));
        using var csvReader = new CsvReader(streamReader, csvReaderConfiguration);

        var anemometerRecords = csvReader.GetRecords<AnemometerCsv>().ToList();

        var temperaturInGradCelsiusAverage = anemometerRecords.Average(r =>
          double.Parse(r.TemperaturInGradCelsius ?? "0", NumberStyles.Any, doubleFormatProvider)
        );

        var luftfeuchtigkeitInProzentAverage = anemometerRecords.Average(r =>
          double.Parse(r.LuftfeuchtigkeitInProzent ?? "0", NumberStyles.Any, doubleFormatProvider)
        );

        var windgeschwindigkeitInMeterProSekundeAverage = anemometerRecords.Average(r =>
          double.Parse(
            r.WindgeschwindigkeitInMeterProSekunde ?? "0",
            NumberStyles.Any,
            doubleFormatProvider
          )
        );

        _logger.LogInformation(
          "*** CSV MessdatenPaket (Nr. {Messnummer}) -> Temperatur [°C]: {TemperaturInGradCelsiusAverage}",
          messdatenPaket.Messnummer,
          temperaturInGradCelsiusAverage
        );
        _logger.LogInformation(
          "*** CSV MessdatenPaket (Nr. {Messnummer}) -> Relative Luftfeuchtigkeit [% r.F.]: {LuftfeuchtigkeitInProzentAverage}",
          messdatenPaket.Messnummer,
          luftfeuchtigkeitInProzentAverage
        );
        _logger.LogInformation(
          "*** CSV MessdatenPaket (Nr. {Messnummer}) -> Windgeschwindigkeit [m/s]: {WindgeschwindigkeitInMeterProSekundeAverage}",
          messdatenPaket.Messnummer,
          windgeschwindigkeitInMeterProSekundeAverage
        );
      }
      else
      {
        _logger.LogWarning("*** CSV MessdatenPaket has no Messdaten");
      }
    }

    ScheduleNewLoadAllCsvMessdatenPaketCommand();
  }

  private void ScheduleNewLoadAllCsvMessdatenPaketCommand()
  {
    Timers.StartSingleTimer(
      nameof(LoadAllCsvMessdatenPaketeCommand),
      new LoadAllCsvMessdatenPaketeCommand(),
      TimeSpan.FromSeconds(15)
    );
  }
}