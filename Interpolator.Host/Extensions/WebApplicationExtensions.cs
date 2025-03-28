using System;
using System.Linq;
using System.Threading;

using Interpolator.Host.Commands;
using Interpolator.Host.Helpers;
using Interpolator.Host.Models;

using Marten;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Interpolator.Host.Extensions;

/// <summary>
/// Extension methods for <see cref="WebApplication"/>.
/// </summary>
public static class WebApplicationExtensions
{
  public static void MapAboutInfo(this WebApplication aWebApplication, string aPath = "about")
  {
    aWebApplication.MapGet(
      aPath,
      () =>
      {
        AboutInfo aboutInfo = AssemblyHelper.GetAboutInfoFromAssembly();

        return Results.Ok(aboutInfo);
      }
    );
  }

  public static void MapMartenUserExample(this WebApplication aWebApplication)
  {
    // You can inject the IDocumentStore and open sessions yourself
    aWebApplication.MapPost(
      "/user",
      async (CreateUserRequest create, [FromServices] IDocumentStore store) =>
      {
        // Open a session for querying, loading, and updating documents
        await using var session = store.LightweightSession();

        var user = new User
        {
          FirstName = create.FirstName,
          LastName = create.LastName,
          Internal = create.Internal,
        };
        session.Store(user);

        await session.SaveChangesAsync();
      }
    );

    aWebApplication.MapGet(
      "/users",
      async (bool internalOnly, [FromServices] IDocumentStore store, CancellationToken ct) =>
      {
        // Open a session for querying documents only
        await using var session = store.QuerySession();

        return await session.Query<User>().Where(x => x.Internal == internalOnly).ToListAsync(ct);
      }
    );

    // OR Inject the session directly to skip the management of the session lifetime
    aWebApplication.MapGet(
      "/user/{id:guid}",
      async (Guid id, [FromServices] IQuerySession session, CancellationToken ct) =>
        await session.LoadAsync<User>(id, ct)
    );
  }

  public static void UseNswag(this WebApplication aWebApplication)
  {
    aWebApplication.UseOpenApi(aSettings =>
      aSettings.PostProcess = (aDocument, _) =>
      {
        AboutInfo aboutInfo = AssemblyHelper.GetAboutInfoFromAssembly();

        aDocument.Info.Title = aboutInfo.Name;
        aDocument.Info.Version = aboutInfo.SemVer;
      }
    );
    aWebApplication.UseSwaggerUi();
  }
}