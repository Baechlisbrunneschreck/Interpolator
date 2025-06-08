namespace Interpolator.Domain.Models;

public class AngereicherterMesspunkt
{
  public AngereicherterMesspunkt(double x, double y, DateTime t)
  {
    X = x;
    Y = y;
    T = t;
  }

  public double A { get; set; }
  public double B { get; set; }
  public double C { get; set; }
  public double D { get; set; }

  // Gewichtung
  public double? P { get; set; }

  public DateTime T { get; }
  public double X { get; }

  public double Y { get; }
  public double Y2 { get; set; }
}
