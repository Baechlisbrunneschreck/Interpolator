using System;

using Marten;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;

using Weasel.Core;

namespace Interpolator.Host.Extensions;

/// <summary>
/// Extension methods for <see cref="WebApplicationBuilder" />.
/// </summary>
public static class WebApplicationBuilderExtensions
{
  public static void SetupMarten(this WebApplicationBuilder webApplicationBuilder)
  {
    // This is the absolute, simplest way to integrate Marten into your
    // .NET application with Marten's default configuration
    webApplicationBuilder.Services.AddMarten(options =>
    {
      // Establish the connection string to your Marten database
      options.Connection(webApplicationBuilder.Configuration.GetConnectionString("Marten")!);

      // Specify that we want to use STJ as our serializer
      options.UseSystemTextJsonForSerialization();

      // If we're running in development mode, let Marten just take care
      // of all necessary schema building and patching behind the scenes
      if (webApplicationBuilder.Environment.IsDevelopment())
      {
        options.AutoCreateSchemaObjects = AutoCreate.All;
      }
    });
  }

  public static void SetupNswag(this WebApplicationBuilder aWebApplicationBuilder)
  {
    aWebApplicationBuilder.Services.AddEndpointsApiExplorer();
    aWebApplicationBuilder.Services.AddOpenApiDocument();
  }

  public static void SetupSerilog(
    this WebApplicationBuilder aWebApplicationBuilder,
    Action<HostBuilderContext, LoggerConfiguration>? aCustomLoggerConfigurationHandler = null
  )
  {
    aWebApplicationBuilder.Host.UseSerilog(
      (aHostBuilderContext, aLoggerConfiguration) =>
      {
        aLoggerConfiguration.ReadFrom.Configuration(aHostBuilderContext.Configuration);

        aCustomLoggerConfigurationHandler?.Invoke(aHostBuilderContext, aLoggerConfiguration);
      }
    );
  }
}