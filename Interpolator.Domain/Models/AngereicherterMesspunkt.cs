namespace Interpolator.Domain.Models;

public class AngereicherterMesspunkt
{
  public AngereicherterMesspunkt(double x, double y)
  {
    X = x;
    Y = y;
  }

  public double X { get; }

  public double Y { get; }

  // Gewichtung
  public double P { get; set; }

  public double A { get; set; }

  public double B { get; set; }

  public double C { get; set; }

  public double D { get; set; }

  public double Y2 { get; set; }
}
