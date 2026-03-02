using System.Threading.Tasks;

namespace InternetBlocker.Core.Interfaces;

public interface IIconService
{
    Task<byte[]?> GetIconAsync(string path);
}
