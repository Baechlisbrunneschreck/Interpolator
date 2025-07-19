using Interpolator.Domain.Extensions;
using Interpolator.Domain.Models;

using SkiaSharp;

namespace Interpolator.Domain.Tests;

public class AngereicherterMesspunktListExtensionsTests
{
  [Fact]
  public void Test1()
  {
    // Arrange
    List<Messpunkt> messListe = new List<Messpunkt>
    {
      new Messpunkt(1, 2, DateTime.UtcNow),
      new Messpunkt(2, 3, DateTime.UtcNow),
      new Messpunkt(3, 5, DateTime.UtcNow),
      new Messpunkt(4, 7, DateTime.UtcNow),
      new Messpunkt(5, 11, DateTime.UtcNow),
      new Messpunkt(6, 13, DateTime.UtcNow),
      new Messpunkt(7, 17, DateTime.UtcNow),
      new Messpunkt(8, 19, DateTime.UtcNow),
      new Messpunkt(9, 23, DateTime.UtcNow),
      new Messpunkt(10, 29, DateTime.UtcNow),
    };
    double gewichtung = 1.0;

    // Act
    IEnumerable<SplineMesspunkt> splineMesspunkte = messListe.ToSplineMesspunkte(gewichtung);
    IEnumerable<Splinepunkt> splinepunkte = splineMesspunkte.ToSplinepunkte(0.1);

    // Assert
    PrintToCsv(splineMesspunkte);
    RenderPointsWithSkiaSharp(
      messListe,
      splinepunkte,
      new DirectoryInfo(Directory.GetCurrentDirectory())
    );
    Assert.True(splineMesspunkte.Any());
    Assert.True(splinepunkte.Any());
  }

  private static void PrintToCsv(IEnumerable<SplineMesspunkt> splineMesspunkte)
  {
    string csvSeparator = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
    string csvPath = Path.Combine(Directory.GetCurrentDirectory(), "spline_messpunkte.csv");
    using StreamWriter writer = new StreamWriter(csvPath);
    writer.WriteLine(
      $"X{csvSeparator}Y{csvSeparator}A{csvSeparator}B{csvSeparator}C{csvSeparator}D"
    );
    foreach (SplineMesspunkt splineMesspunkt in splineMesspunkte)
    {
      writer.WriteLine(
        splineMesspunkt.X
          + csvSeparator
          + splineMesspunkt.Y
          + csvSeparator
          + splineMesspunkt.A
          + csvSeparator
          + splineMesspunkt.B
          + csvSeparator
          + splineMesspunkt.C
          + csvSeparator
          + splineMesspunkt.D
      );
    }
  }

  private static void RenderPointsWithSkiaSharp(
    List<Messpunkt> originalPoints,
    IEnumerable<Splinepunkt> interpolatedPoints,
    DirectoryInfo? outputDirectory = null
  )
  {
    const int width = 800;
    const int height = 600;

    // Create bitmap
    using SKBitmap bitmap = new SKBitmap(width, height);
    using SKCanvas canvas = new SKCanvas(bitmap);

    // Clear background
    canvas.Clear(SKColors.White);

    // Create paints
    using SKPaint originalPointPaint = new SKPaint
    {
      Color = SKColors.Red,
      IsAntialias = true,
      Style = SKPaintStyle.Fill,
    };

    using SKPaint interpolatedLinePaint = new SKPaint
    {
      Color = SKColors.Blue,
      IsAntialias = true,
      Style = SKPaintStyle.Stroke,
      StrokeWidth = 2,
    };

    using SKPaint gridPaint = new SKPaint
    {
      Color = SKColors.LightGray,
      IsAntialias = true,
      Style = SKPaintStyle.Stroke,
      StrokeWidth = 1,
    };

    // Find data bounds
    List<Messpunkt> allPoints = originalPoints
      .Concat(interpolatedPoints.Select(p => new Messpunkt(p.X, p.Y, p.T)))
      .ToList();
    double minX = allPoints.Min(p => p.X);
    double maxX = allPoints.Max(p => p.X);
    double minY = allPoints.Min(p => p.Y);
    double maxY = allPoints.Max(p => p.Y);

    // Add some padding
    double rangeX = maxX - minX;
    double rangeY = maxY - minY;
    minX -= rangeX * 0.1;
    maxX += rangeX * 0.1;
    minY -= rangeY * 0.1;
    maxY += rangeY * 0.1;

    // Helper function to convert data coordinates to screen coordinates
    float ToScreenX(double x) => (float)((x - minX) / (maxX - minX) * (width - 80) + 40);
    float ToScreenY(double y) => (float)(height - 40 - (y - minY) / (maxY - minY) * (height - 80));

    // Draw grid
    for (int i = 0; i <= 10; i++)
    {
      float x = 40 + i * (width - 80) / 10f;
      float y = 40 + i * (height - 80) / 10f;
      canvas.DrawLine(x, 40, x, height - 40, gridPaint);
      canvas.DrawLine(40, y, width - 40, y, gridPaint);
    }

    // Draw interpolated line
    Splinepunkt[] interpolatedArray = interpolatedPoints.ToArray();
    if (interpolatedArray.Length > 1)
    {
      using SKPath path = new SKPath();
      Splinepunkt firstPoint = interpolatedArray[0];
      path.MoveTo(ToScreenX(firstPoint.X), ToScreenY(firstPoint.Y));

      foreach (Splinepunkt? point in interpolatedArray.Skip(1))
      {
        path.LineTo(ToScreenX(point.X), ToScreenY(point.Y));
      }

      canvas.DrawPath(path, interpolatedLinePaint);
    }

    // Draw circles for interpolated points
    foreach (Splinepunkt? point in interpolatedArray)
    {
      float x = ToScreenX(point.X);
      float y = ToScreenY(point.Y);
      canvas.DrawCircle(x, y, 4, interpolatedLinePaint);
    }

    // Draw original points
    foreach (Messpunkt point in originalPoints)
    {
      float x = ToScreenX(point.X);
      float y = ToScreenY(point.Y);
      canvas.DrawCircle(x, y, 6, originalPointPaint);
    }

    // Save image to test output
    string outputPath = Path.Combine(
      outputDirectory?.FullName ?? Directory.GetCurrentDirectory(),
      "interpolation_plot.png"
    );
    using SKImage image = SKImage.FromBitmap(bitmap);
    using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
    using FileStream stream = File.OpenWrite(outputPath);
    data.SaveTo(stream);

    Console.WriteLine($"Plot saved to: {outputPath}");
  }
}
