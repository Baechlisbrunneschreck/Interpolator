namespace Interpolator.Domain.Models;

public class SplineMesspunkt
{
  public SplineMesspunkt(double x, double y, DateTime t)
  {
    X = x;
    Y = y;
    T = t;
  }

  // Polinomiale Parameter
  // y = Ax^3 + Bx^2 + Cx + D
  public double A { get; set; }
  public double B { get; set; }
  public double C { get; set; }
  public double D { get; set; }

  // Gewichtung
  public double? P { get; set; }

  public DateTime T { get; }
  public double X { get; }

  public double Y { get; }

  // y''(x) = dy^2/dx^2  2. Ableitung
  public double Y2 { get; set; }
}
