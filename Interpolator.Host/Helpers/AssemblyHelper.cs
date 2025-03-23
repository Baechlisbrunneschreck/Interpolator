using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Interpolator.Host.Models;

namespace Interpolator.Host.Helpers;

/// <summary>
/// Provides helper methods in relation to <see cref="Assembly"/>.
/// </summary>
public static class AssemblyHelper
{
  public static AboutInfo GetAboutInfoFromAssembly(Assembly? aAssembly = null)
  {
    Assembly asm = aAssembly ?? GetFallbackAssembly();
    AssemblyName? asmName = asm.GetName();
    string name = asmName?.Name ?? "Unknown Name";
    System.Version version = asmName?.Version ?? new System.Version("0.1.0");
    string? informationalVersion = GetInformationalVersion(aAssembly);
    string semver = informationalVersion?.Split('+').FirstOrDefault() ?? "Unknown SemVer";
    string sha = informationalVersion?.Split("Sha.").LastOrDefault() ?? "Unknown Sha";

    return new AboutInfo(name, version, semver, sha, informationalVersion ?? "Unknown Informational Version");
  }

  public static AssemblyName GetAssemblyName(Assembly? aAssembly = null)
  {
    Assembly asm = aAssembly ?? GetFallbackAssembly();

    return asm.GetName();
  }

  public static string GetInformationalVersion(Assembly? aAssembly = null)
  {
    Assembly asm = aAssembly ?? GetFallbackAssembly();

    return FileVersionInfo.GetVersionInfo(asm.Location).ProductVersion;
  }

  private static Assembly GetFallbackAssembly()
  {
    return Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
  }
}
