namespace Interpolator.Domain.Models;

public class Messpunkt
{
  public double X { get; set; }
  public double Y { get; set; }
  public DateTime T { get; set; }

  public Messpunkt(double x, double y, DateTime t)
  {
    X = x;
    Y = y;
    T = t;
  }
}
