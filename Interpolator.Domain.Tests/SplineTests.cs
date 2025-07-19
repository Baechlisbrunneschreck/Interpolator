using System.Globalization;

using CsvHelper.Configuration;

using Interpolator.Domain.Extensions;
using Interpolator.Domain.Models;
using Interpolator.Domain.Tests.Helpers;
using Interpolator.Host.Models.Csv;

namespace Interpolator.Domain.Tests;

public class SplineTests
{
  [Fact]
  public void SimpleSplineTest()
  {
    // Arrange
    List<Messpunkt> messpunkte = new List<Messpunkt>
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
    IEnumerable<SplineMesspunkt> splineMesspunkte = messpunkte.ToSplineMesspunkte(gewichtung);
    IEnumerable<Splinepunkt> splinepunkte = splineMesspunkte.ToSplinepunkte(abstand);

    // Assert
    SimpleCsvHelper.PrintToCsv(splineMesspunkte);
    SimpleCsvHelper.PrintToCsv(splinepunkte);
    RenderHelper.RenderPointsWithSkiaSharp(
      messpunkte,
      splinepunkte,
      new DirectoryInfo(Directory.GetCurrentDirectory())
    );
    Assert.True(splineMesspunkte.Any());
    Assert.True(splinepunkte.Any());
  }

  [Fact]
  public void SplineTestFromFileInput()
  {
    // Arrange
    const double gewichtung = 10.0E-13;
    const double abstand = 0.1;
    CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
      NewLine = Environment.NewLine,
      Delimiter = ";",
    };
    using StreamReader streamReader = new StreamReader(Path.Combine(Directory.GetCurrentDirectory(), "EFH_Vergleich_03_10_03.txt"));
    using CsvHelper.CsvReader csvReader = new CsvHelper.CsvReader(streamReader, config);
    IEnumerable<S0AnemometerCsv> s0AnemometerRecords = csvReader.GetRecords<S0AnemometerCsv>();
    IEnumerable<Messpunkt> messpunkte = s0AnemometerRecords.Select(
      record => new Messpunkt(
        double.Parse(record.X ?? "0", CultureInfo.InvariantCulture),
        double.Parse(record.Y ?? "0", CultureInfo.InvariantCulture),
        DateTime.UtcNow
      )
    );

    // Act
    IEnumerable<SplineMesspunkt> splineMesspunkte = messpunkte.ToSplineMesspunkte(gewichtung);
    IEnumerable<Splinepunkt> splinepunkte = splineMesspunkte.ToSplinepunkte(abstand);

    // Assert
    RenderHelper.RenderPointsWithSkiaSharp(
      messpunkte,
      splinepunkte,
      new DirectoryInfo(Directory.GetCurrentDirectory())
    );
    Assert.True(splineMesspunkte.Any());
    Assert.True(splinepunkte.Any());
  }
}
