using System;

using CsvHelper.Configuration.Attributes;

namespace Interpolator.Host.Models.Csv;

public class AnemometerCsv
{
  [Index(2)]
  public string? LuftfeuchtigkeitInProzent { get; set; }

  [Index(1)]
  public string? TemperaturInGradCelsius { get; set; }

  [Index(3)]
  public string? WindgeschwindigkeitInMeterProSekunde { get; set; }

  [Index(0)]
  public string? Datum { get; set; }
}