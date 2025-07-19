using Interpolator.Domain.Models;
using SkiaSharp;

namespace Interpolator.Domain.Tests.Helpers;

internal static class RenderHelper
{
  internal static void RenderPointsWithSkiaSharp(
    List<Messpunkt> originalPoints,
    IEnumerable<Splinepunkt> interpolatedPoints,
    DirectoryInfo? outputDirectory = null,
    bool drawOriginalPoints = true,
    bool drawMeasurementPointsOnSpline = true
  )
  {
    const int width = 1920;
    const int height = 1080;

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

    if (drawMeasurementPointsOnSpline)
    {
      foreach (Splinepunkt? point in interpolatedArray)
      {
        float x = ToScreenX(point.X);
        float y = ToScreenY(point.Y);
        canvas.DrawCircle(x, y, 4, interpolatedLinePaint);
      }
    }

    if (drawOriginalPoints)
    {
      foreach (Messpunkt point in originalPoints)
      {
        float x = ToScreenX(point.X);
        float y = ToScreenY(point.Y);
        canvas.DrawCircle(x, y, 6, originalPointPaint);
      }
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
