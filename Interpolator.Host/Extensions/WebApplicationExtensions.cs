using Interpolator.Host.Helpers;
using Interpolator.Host.Models;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

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
}
