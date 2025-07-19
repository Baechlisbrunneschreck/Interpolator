using Interpolator.Domain.Models;

namespace Interpolator.Domain.Tests.Helpers;

internal static class CsvHelper
{
  internal static void PrintToCsv(IEnumerable<Splinepunkt> splinepunkte)
  {
    string csvSeparator = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
    string csvPath = Path.Combine(Directory.GetCurrentDirectory(), "splinepunkte.csv");
    using StreamWriter writer = new StreamWriter(csvPath);
    writer.WriteLine($"X{csvSeparator}Y{csvSeparator}T");
    foreach (Splinepunkt splinepunkt in splinepunkte)
    {
      writer.WriteLine(splinepunkt.X + csvSeparator + splinepunkt.Y + csvSeparator + splinepunkt.T);
    }
  }

  internal static void PrintToCsv(IEnumerable<SplineMesspunkt> splineMesspunkte)
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
}
