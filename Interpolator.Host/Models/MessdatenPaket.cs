using System;

namespace Interpolator.Host.Models;

public class MessdatenPaket
{
  public string? Bemerkungen { get; set; }

  public Guid Id { get; set; }

  public Messart Messart { get; set; }

  public byte[]? Messdaten { get; set; }

  public string MessdatenMimeType { get; set; } = "application/octet-stream";

  public string? Messnummer { get; set; }

  public string? Messort { get; set; }
}