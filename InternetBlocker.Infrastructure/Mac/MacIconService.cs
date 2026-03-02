using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using InternetBlocker.Core.Interfaces;

namespace InternetBlocker.Infrastructure.Mac;

public class MacIconService : IIconService
{
    public async Task<byte[]?> GetIconAsync(string processPath)
    {
        if (string.IsNullOrEmpty(processPath) || !File.Exists(processPath))
            return null;

        try
        {
            // macOS apps are usually .app bundles. If path is to the binary inside, 
            // we should try to find the bundle icon.
            string? appBundlePath = GetAppBundlePath(processPath);
            string? iconPath = null;

            if (!string.IsNullOrEmpty(appBundlePath))
            {
                // Find .icns from Info.plist
                iconPath = await GetIconPathFromPlistAsync(appBundlePath);
            }

            if (string.IsNullOrEmpty(iconPath)) return null;

            // Convert .icns to .png using 'sips' (native macOS tool)
            string tempPng = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");
            
            var startInfo = new ProcessStartInfo
            {
                FileName = "sips",
                Arguments = $"-s format png \"{iconPath}\" --out \"{tempPng}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                if (process.ExitCode == 0 && File.Exists(tempPng))
                {
                    byte[] data = await File.ReadAllBytesAsync(tempPng);
                    File.Delete(tempPng);
                    return data;
                }
            }
        }
        catch { }

        return null;
    }

    private string? GetAppBundlePath(string binaryPath)
    {
        // Binary is usually at .../Contents/MacOS/BinaryName
        var directory = Path.GetDirectoryName(binaryPath);
        while (!string.IsNullOrEmpty(directory) && directory != "/")
        {
            if (directory.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
                return directory;
            directory = Path.GetDirectoryName(directory);
        }
        return null;
    }

    private async Task<string?> GetIconPathFromPlistAsync(string bundlePath)
    {
        try
        {
            string plistPath = Path.Combine(bundlePath, "Contents", "Info.plist");
            if (!File.Exists(plistPath)) return null;

            // Simple grep for CFBundleIconFile in Info.plist
            // A more robust way would be using 'defaults read' or a plist parser
            var startInfo = new ProcessStartInfo
            {
                FileName = "defaults",
                Arguments = $"read \"{plistPath}\" CFBundleIconFile",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return null;

            string iconFileName = (await process.StandardOutput.ReadToEndAsync()).Trim();
            if (string.IsNullOrEmpty(iconFileName)) return null;

            if (!iconFileName.EndsWith(".icns")) iconFileName += ".icns";

            string fullIconPath = Path.Combine(bundlePath, "Contents", "Resources", iconFileName);
            return File.Exists(fullIconPath) ? fullIconPath : null;
        }
        catch { return null; }
    }
}
