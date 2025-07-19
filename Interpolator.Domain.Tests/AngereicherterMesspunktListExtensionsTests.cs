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
    var messListe = new List<Messpunkt>
    {
      new Messpunkt(1, 2, DateTime.UtcNow),
      new Messpunkt(2, 3, DateTime.UtcNow),
      new Messpunkt(3, 5, DateTime.UtcNow),
      new Messpunkt(4, 7, DateTime.UtcNow)
    };
    double gewichtung = 1.0;

    // Act
    IEnumerable<SplineMesspunkt> splineMesspunkte = messListe.ToSplineMesspunkte(gewichtung);
    IEnumerable<Splinepunkt> splinepunkte = splineMesspunkte.ToSplinepunkte(0.1);

    // Quick SkiaSharp Rendering
    RenderPointsWithSkiaSharp(messListe, splinepunkte);

    // Assert
    Assert.True(splineMesspunkte.Any());
    Assert.True(splinepunkte.Any());
  }

  private void RenderPointsWithSkiaSharp(List<Messpunkt> originalPoints, IEnumerable<Splinepunkt> interpolatedPoints)
  {
    const int width = 800;
    const int height = 600;

    // Create bitmap
    using var bitmap = new SKBitmap(width, height);
    using var canvas = new SKCanvas(bitmap);

    // Clear background
    canvas.Clear(SKColors.White);

    // Create paints
    using var originalPointPaint = new SKPaint
    {
      Color = SKColors.Red,
      IsAntialias = true,
      Style = SKPaintStyle.Fill
    };

    using var interpolatedLinePaint = new SKPaint
    {
      Color = SKColors.Blue,
      IsAntialias = true,
      Style = SKPaintStyle.Stroke,
      StrokeWidth = 2
    };

    using var gridPaint = new SKPaint
    {
      Color = SKColors.LightGray,
      IsAntialias = true,
      Style = SKPaintStyle.Stroke,
      StrokeWidth = 1
    };

    // Find data bounds
    var allPoints = originalPoints.Concat(interpolatedPoints.Select(p => new Messpunkt(p.X, p.Y, p.T))).ToList();
    var minX = allPoints.Min(p => p.X);
    var maxX = allPoints.Max(p => p.X);
    var minY = allPoints.Min(p => p.Y);
    var maxY = allPoints.Max(p => p.Y);

    // Add some padding
    var rangeX = maxX - minX;
    var rangeY = maxY - minY;
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
    var interpolatedArray = interpolatedPoints.ToArray();
    if (interpolatedArray.Length > 1)
    {
      using var path = new SKPath();
      var firstPoint = interpolatedArray[0];
      path.MoveTo(ToScreenX(firstPoint.X), ToScreenY(firstPoint.Y));

      foreach (var point in interpolatedArray.Skip(1))
      {
        path.LineTo(ToScreenX(point.X), ToScreenY(point.Y));
      }

      canvas.DrawPath(path, interpolatedLinePaint);
    }

    // Draw circles for interpolated points
    foreach (var point in interpolatedArray)
    {
      float x = ToScreenX(point.X);
      float y = ToScreenY(point.Y);
      canvas.DrawCircle(x, y, 4, interpolatedLinePaint);
    }

    // Draw original points
    foreach (var point in originalPoints)
    {
      float x = ToScreenX(point.X);
      float y = ToScreenY(point.Y);
      canvas.DrawCircle(x, y, 6, originalPointPaint);
    }

    // Save image to test output
    var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "interpolation_plot.png");
    using var image = SKImage.FromBitmap(bitmap);
    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
    using var stream = File.OpenWrite(outputPath);
    data.SaveTo(stream);

    Console.WriteLine($"Plot saved to: {outputPath}");
  }
}
