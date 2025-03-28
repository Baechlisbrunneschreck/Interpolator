using System.IO;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace Interpolator.Host.Extensions;

public static class IFormFileExtensions
{
  public static async Task<byte[]> ConvertToByteArrayAsync(this IFormFile file)
  {
    using (var memoryStream = new MemoryStream())
    {
      await file.CopyToAsync(memoryStream);
      return memoryStream.ToArray();
    }
  }
}