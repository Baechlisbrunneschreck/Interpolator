using System;

using Serilog;

namespace Interpolator.Host.Helpers;

/// <summary>
/// Provides helper methods in relation to <see cref="Serilog"/>.
/// </summary>
public static class LoggerHelper
{
  public static void RunWithSerilog(Action<ILogger> aAction, string? aBootstrapLogFilePath = null)
  {
    Log.Logger = new LoggerConfiguration()
      .WriteTo.Console()
      .WriteTo.File(aBootstrapLogFilePath ?? "Logs/bootstrap-.txt", rollingInterval: RollingInterval.Day)
      .CreateBootstrapLogger();

    Log.Information("*** Starting up ASP.NET Host...");

    try
    {
      aAction.Invoke(Log.Logger);
    }
    catch (Exception ex)
    {
      Log.Fatal(ex, "*** Unhandled exception!");
    }
    finally
    {
      Log.Information("*** Host Shut down complete!");
      Log.CloseAndFlush();
    }
  }
}