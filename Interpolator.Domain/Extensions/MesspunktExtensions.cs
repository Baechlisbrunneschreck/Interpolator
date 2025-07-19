using Interpolator.Domain.Models;

namespace Interpolator.Domain.Extensions;

public static class MesspunktExtensions
{
  public static IEnumerable<SplineMesspunkt> ToAngereicherteMesspunkte(
    this IEnumerable<Messpunkt> messpunkte, double gewichtung)
  {
    return messpunkte.Select(m => new SplineMesspunkt(m.X, m.Y, m.T)
    {
      P = gewichtung
    });
  }
}