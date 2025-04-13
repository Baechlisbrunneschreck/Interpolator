using System;

namespace Interpolator.Host.Models.Aggregates;

public record PhotovoltaikMessung
{
  public double LeistungInWatt { get; set; }

  public double LuftdruckInHektoPascal { get; set; }

  public double LuftfeuchtigkeitInProzent { get; set; }

  public string? Messort { get; set; }

  public double SonnenhoeheInGrad { get; set; }

  public double SonnenrichtungInGrad { get; set; }

  public double TaupunktInGradCelsius { get; set; }

  public double TemperaturInGradCelsius { get; set; }

  public DateTime Zeitstempel { get; set; }
}