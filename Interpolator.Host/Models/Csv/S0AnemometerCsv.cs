using CsvHelper.Configuration.Attributes;

namespace Interpolator.Host.Models.Csv;

public class S0AnemometerCsv
{
  [Index(0)]
  public string? Uhrzeit { get; set; }

  [Index(1)]
  public string? LeistungInWatt { get; set; }

  [Index(3)]
  public string? X { get; set; }

  [Index(4)]
  public string? Y { get; set; }

  [Index(5)]
  public string? P { get; set; }
}
