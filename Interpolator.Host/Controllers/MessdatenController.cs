using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Akka.Actor;
using Akka.Hosting;

using CsvHelper;
using CsvHelper.Configuration;

using Interpolator.Host.Actors;
using Interpolator.Host.Controllers.Messages;
using Interpolator.Host.Extensions;
using Interpolator.Host.Models;
using Interpolator.Host.Models.Aggregates;

using Marten;

using Microsoft.AspNetCore.Mvc;

namespace Interpolator.Host.Controllers;

[Route("api/[controller]")]
public class MessdatenController : ControllerBase
{
  private readonly IDocumentStore _documentStore;
  private readonly IRequiredActor<MessdatenPaketLoaderActor2> _messdatenActorRef;

  public MessdatenController(
    IDocumentStore documentStore,
    IRequiredActor<MessdatenPaketLoaderActor2> messdatenActorRef
  )
  {
    _documentStore = documentStore;
    _messdatenActorRef = messdatenActorRef;
  }

  [HttpPost]
  [Route("neue-messdaten")]
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
      Abschlusszeitpunkt = createMessdatenRequest.Abschlusszeitpunkt ?? DateTime.UtcNow,
      Bemerkungen = createMessdatenRequest.Bemerkungen,
    };

    session.Store(neuesMessdatenPaket);

    await session.SaveChangesAsync();

    _messdatenActorRef.ActorRef.Tell(new LoadAllPhotovoltaikMessungenCommand());

    return Ok(neuesMessdatenPaket.Id);
  }

  [HttpGet]
  [Route("uebersicht")]
  public async Task<
    ActionResult<IReadOnlyList<MessdatenPaketUebersichtResponse>>
  > GetMessdatenUebersicht(CancellationToken cancellationToken)
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

  [HttpGet]
  [Route("neuberechnen")]
  public async Task<IActionResult> Neuberechnen()
  {
    _messdatenActorRef.ActorRef.Tell(new LoadAllPhotovoltaikMessungenCommand());

    return Ok();
  }

  [HttpGet]
  [Route("photovoltaik-messungen-download")]
  public async Task<IActionResult> PhotovoltaikMessungenDownload()
  {
    await using var session = _documentStore.QuerySession();
    var photovoltaikMessungen = await session.Query<PhotovoltaikMessung>().ToListAsync();
    var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
    using (var writer = new StreamWriter(tempFilePath))
    {
      using var csv = new CsvWriter(
        writer,
        new CsvConfiguration(CultureInfo.InvariantCulture)
        {
          Delimiter = ";",
          HasHeaderRecord = true,
          IgnoreBlankLines = true,
          TrimOptions = TrimOptions.Trim,
        }
      );
      await csv.WriteRecordsAsync(photovoltaikMessungen);
      await writer.FlushAsync();
    }

    var reader = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read);

    return File(reader, "text/csv", "photovoltaik-messungen.csv");
  }
}