using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using InternetBlocker.Core.Interfaces;

namespace InternetBlocker.Infrastructure.Windows;

public class WindowsIconService : IIconService
{
    public Task<byte[]?> GetIconAsync(string path)
    {
        return Task.Run(() =>
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    return null;

                using var icon = Icon.ExtractAssociatedIcon(path);
                if (icon == null) return null;

                using var ms = new MemoryStream();
                icon.ToBitmap().Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        });
    }
}
