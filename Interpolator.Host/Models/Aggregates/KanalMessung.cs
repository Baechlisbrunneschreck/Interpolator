using System;

using CsvHelper.Configuration.Attributes;

namespace Interpolator.Host.Models.Aggregates;

public class KanalMessung
{
  public Guid Id { get; set; }
  public double TemperaturInGradCelsius { get; set; }
  public double LuftfeuchtigkeitInProzent { get; set; }
  public double WindgeschwindigkeitInMeterProSekunde { get; set; }
  public double LuftdruckInHektoPascal { get; set; }
  public double TaupunktInGradCelsius { get; set; }
  public string? Messort { get; set; }

  [Format("yyyy-MM-dd HH:mm:ss:ffffff")]
  public DateTime Zeitstempel { get; set; }
}
