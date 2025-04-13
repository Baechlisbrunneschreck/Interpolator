using CsvHelper.Configuration.Attributes;

namespace Interpolator.Host.Models.Csv;

public class StromleistungCsv
{
  [Index(1)]
  public string? LeistungInWatt { get; set; }

  [Index(0)]
  public string? Uhrzeit { get; set; }
}