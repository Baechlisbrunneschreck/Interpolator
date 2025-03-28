using System.Text.Json.Serialization;

using Interpolator.Host.Extensions;
using Interpolator.Host.Helpers;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Interpolator.Host;

public static class Program
{
  public static void Main(string[] args)
  {
    LoggerHelper.RunWithSerilog(rootLogger =>
    {
      WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

      builder
        .Services.AddControllersWithViews()
        .AddJsonOptions(options =>
          options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter())
        );
      builder.SetupSerilog();
      builder.SetupNswag();
      builder.SetupMarten();

      WebApplication app = builder.Build();

      app.UseNswag();

      app.MapAboutInfo();
      app.MapMartenUserExample();
      app.MapControllers();
      app.MapGet("/", () => Results.Ok("Hello, World!"));

      app.Run();
    });
  }
}