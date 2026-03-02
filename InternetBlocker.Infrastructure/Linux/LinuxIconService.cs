using System.Threading.Tasks;
using InternetBlocker.Core.Interfaces;

namespace InternetBlocker.Infrastructure.Linux;

public class LinuxIconService : IIconService
{
    public Task<byte[]?> GetIconAsync(string path)
    {
        // On Linux, icons are usually separate files or in desktop entries.
        // For a simple prototype, we return null.
        return Task.FromResult<byte[]?>(null);
    }
}
