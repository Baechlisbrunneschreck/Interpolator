using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Interpolator.Host.Extensions;
using Interpolator.Host.Models;

using Marten;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Interpolator.Host.Controllers;

[Route("api/[controller]")]
public class MessdatenController : ControllerBase
{
  private readonly IDocumentStore _documentStore;

  public MessdatenController(IDocumentStore documentStore)
  {
    _documentStore = documentStore;
  }

  [HttpPost]
  [Route("create")]
  public async Task<ActionResult<Guid>> CreateMessdatenPost(
    [FromForm] CreateMessdatenRequest createMessdatenRequest
  )
  {
    // Open a session for querying, loading, and updating documents
    await using var session = _documentStore.LightweightSession();
    var neuesMessdatenPaket = new MessdatenPaket
    {
      Id = Guid.NewGuid(),
      Messart = createMessdatenRequest.Messart,
      Messdaten = await createMessdatenRequest.Messdaten.ConvertToByteArrayAsync(),
      MessdatenMimeType = createMessdatenRequest.Messdaten.ContentType,
      Messnummer = createMessdatenRequest.Messnummer,
      Messort = createMessdatenRequest.Messort,
    };
    session.Store(neuesMessdatenPaket);

    await session.SaveChangesAsync();

    return Ok(neuesMessdatenPaket.Id);
  }

  [HttpGet]
  [Route("uebersicht")]
  public async Task<ActionResult<IReadOnlyList<MessdatenPaketUebersichtResponse>>> GetMessdatenUebersicht(
    CancellationToken cancellationToken
  )
  {
    await using var session = _documentStore.QuerySession();
    var responsePayload = await session
      .Query<MessdatenPaket>()
      .Select(messdatenPaket => new MessdatenPaketUebersichtResponse(
        messdatenPaket.Id,
        messdatenPaket.Messnummer,
        messdatenPaket.Messart,
        messdatenPaket.MessdatenMimeType,
        messdatenPaket.Messort
      ))
      .ToListAsync(cancellationToken);

    return Ok(responsePayload);
  }
}

public record MessdatenPaketUebersichtResponse(
  Guid Id,
  string? Messnummer,
  Messart Messart,
  string MessdatenMimeType,
  string? Messort
);

public record CreateMessdatenRequest(
  string Messnummer,
  string Messort,
  Messart Messart,
  DateOnly Abschlussdatum,
  IFormFile Messdaten
);