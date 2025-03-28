using System;
using System.Linq;
using System.Threading;

using Interpolator.Host.Extensions;
using Interpolator.Host.Helpers;

using Marten;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Weasel.Core;

namespace Interpolator.Host;

public static class Program
{
  public static void Main(string[] args)
  {
    LoggerHelper.RunWithSerilog(rootLogger =>
    {
      WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

      builder.SetupSerilog();
      builder.SetupNswag();

      // This is the absolute, simplest way to integrate Marten into your
      // .NET application with Marten's default configuration
      builder.Services.AddMarten(options =>
      {
        // Establish the connection string to your Marten database
        options.Connection(builder.Configuration.GetConnectionString("Marten")!);

        // Specify that we want to use STJ as our serializer
        options.UseSystemTextJsonForSerialization();

        // If we're running in development mode, let Marten just take care
        // of all necessary schema building and patching behind the scenes
        if (builder.Environment.IsDevelopment())
        {
          options.AutoCreateSchemaObjects = AutoCreate.All;
        }
      });

      WebApplication app = builder.Build();

      app.UseNswag();

      app.MapAboutInfo();
      app.MapGet("/", () => "Hello World!");
      // You can inject the IDocumentStore and open sessions yourself
      app.MapPost(
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

      app.MapGet(
        "/users",
        async (bool internalOnly, [FromServices] IDocumentStore store, CancellationToken ct) =>
        {
          // Open a session for querying documents only
          await using var session = store.QuerySession();

          return await session.Query<User>().Where(x => x.Internal == internalOnly).ToListAsync(ct);
        }
      );

      // OR Inject the session directly to skip the management of the session lifetime
      app.MapGet(
        "/user/{id:guid}",
        async (Guid id, [FromServices] IQuerySession session, CancellationToken ct) =>
          await session.LoadAsync<User>(id, ct)
      );

      app.Run();
    });
  }
}

public record CreateUserRequest(string? FirstName, string? LastName, bool Internal);

public class User
{
  public Guid Id { get; set; }

  public string? FirstName { get; set; }

  public string? LastName { get; set; }

  public bool Internal { get; set; }
}