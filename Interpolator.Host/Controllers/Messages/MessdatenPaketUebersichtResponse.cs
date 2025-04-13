using System;

using Interpolator.Host.Models;

namespace Interpolator.Host.Controllers.Messages;

public record MessdatenPaketUebersichtResponse(
  Guid Id,
  string? Messnummer,
  Messart Messart,
  string MessdatenMimeType,
  string? Messort
);