namespace Interpolator.Domain.Models;

public class Interpolationspunkt
{
  public double X { get; }

  public double Y { get; }

  public Interpolationspunkt(AngereicherterMesspunkt messpunkt, double offset)
  {
    X = messpunkt.X + offset;
    Y = messpunkt.A * Math.Pow(X, 3) + messpunkt.B * Math.Pow(X, 2) + messpunkt.C * X + messpunkt.D;
  }
}
