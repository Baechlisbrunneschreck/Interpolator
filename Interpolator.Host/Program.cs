using Interpolator.Host.Extensions;
using Interpolator.Host.Helpers;

using Microsoft.AspNetCore.Builder;

namespace Interpolator.Host;

public static class Program
{
  public static void Main(string[] args)
  {
    LoggerHelper.RunWithSerilog(rootLogger =>
    {
      WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

      builder.SetupSerilog();

      WebApplication app = builder.Build();

      app.MapAboutInfo();
      app.MapGet("/", () => "Hello World!");

      app.Run();
    });
  }
}
