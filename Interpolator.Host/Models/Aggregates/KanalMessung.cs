using System;

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
  public DateTime Zeitstempel { get; set; }
}
