namespace Interpolator.Domain.Models;

public class Splinepunkt
{
  public Splinepunkt(SplineMesspunkt messpunkt, double x, DateTime t)
  {
    X = x;
    Y =
      (messpunkt.A * Math.Pow(X, 3))
      + (messpunkt.B * Math.Pow(X, 2))
      + (messpunkt.C * X)
      + messpunkt.D;
    T = t;
  }

  public DateTime T { get; }
  public double X { get; }

  public double Y { get; }
}
