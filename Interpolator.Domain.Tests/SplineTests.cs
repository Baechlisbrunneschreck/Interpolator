using Interpolator.Domain.Extensions;
using Interpolator.Domain.Models;
using Interpolator.Domain.Tests.Helpers;

namespace Interpolator.Domain.Tests;

public class SplineTests
{
  [Fact]
  public void Test1()
  {
    // Arrange
    List<Messpunkt> messListe = new List<Messpunkt>
    {
      new Messpunkt(0, 20, DateTime.UtcNow),
      new Messpunkt(10, 30, DateTime.UtcNow),
      new Messpunkt(20, 170, DateTime.UtcNow),
      new Messpunkt(30, 50, DateTime.UtcNow),
      new Messpunkt(40, 70, DateTime.UtcNow),
      new Messpunkt(50, 230, DateTime.UtcNow),
      new Messpunkt(60, 110, DateTime.UtcNow),
      new Messpunkt(70, 130, DateTime.UtcNow),
      new Messpunkt(80, 290, DateTime.UtcNow),
      new Messpunkt(90, 190, DateTime.UtcNow),
    };
    const double gewichtung = 1.0;
    const double abstand = 0.1;

    // Act
    IEnumerable<SplineMesspunkt> splineMesspunkte = messListe.ToSplineMesspunkte(gewichtung);
    IEnumerable<Splinepunkt> splinepunkte = splineMesspunkte.ToSplinepunkte(abstand);

    // Assert
    CsvHelper.PrintToCsv(splineMesspunkte);
    CsvHelper.PrintToCsv(splinepunkte);
    RenderHelper.RenderPointsWithSkiaSharp(
      messListe,
      splinepunkte,
      new DirectoryInfo(Directory.GetCurrentDirectory())
    );
    Assert.True(splineMesspunkte.Any());
    Assert.True(splinepunkte.Any());
  }
}
