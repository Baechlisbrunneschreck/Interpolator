using System;

using Interpolator.Host.Models;

using Microsoft.AspNetCore.Http;

namespace Interpolator.Host.Controllers.Messages;

public record CreateMessdatenRequest(
  string Messnummer,
  string Messort,
  Messart Messart,
  IFormFile Messdaten,
  DateTime? Abschlusszeitpunkt,
  string? Bemerkungen
);