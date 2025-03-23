using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

using Serilog;

namespace Interpolator.Host.Extensions;

/// <summary>
/// Extension methods for <see cref="WebApplicationBuilder" />.
/// </summary>
public static class WebApplicationBuilderExtensions
{
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
