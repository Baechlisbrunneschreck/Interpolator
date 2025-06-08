using System.Globalization;

using Akka.Actor;

using CsvHelper.Configuration;

namespace Interpolator.Host.Actors.Bases;

public abstract class CsvLoaderActorBase : UntypedActor
{
  private readonly NumberFormatInfo _doubleFormatProvider = new() { NumberDecimalSeparator = "," };

  protected readonly CsvConfiguration _csvReaderConfiguration = new(CultureInfo.InvariantCulture)
  {
    Delimiter = ";",
    HasHeaderRecord = true,
    IgnoreBlankLines = true,
    TrimOptions = TrimOptions.Trim,
  };

  protected double GetDouble(string? input)
  {
    return double.Parse(input ?? "0", NumberStyles.Any, _doubleFormatProvider);
  }
}
