namespace Interpolator.Domain.Models;

public class Splinepunkt
{
  public Splinepunkt(SplineMesspunkt messpunkt, double x, DateTime t)
  {
    var deltaX = x - messpunkt.X;
    var param1 = messpunkt.A * deltaX * deltaX * deltaX;
    var param2 = messpunkt.B * deltaX * deltaX;
    var param3 = messpunkt.C * deltaX;
    var param4 = messpunkt.D;
    var y = param1 + param2 + param3 + param4;

    X = x;
    Y = y;
    T = t;
  }

  public DateTime T { get; }
  public double X { get; }

  public double Y { get; }
}
