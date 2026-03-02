using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InternetBlocker.Core.Interfaces;
using InternetBlocker.Core.Models;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace InternetBlocker.ViewModels;

public partial class BlockedViewModel : ViewModelBase
{
    private readonly IFirewallService _firewallService;
    private readonly IBlockedEntityRepository _repository;

    public ObservableCollection<BlockedEntity> BlockedEntities { get; } = new();

    public BlockedViewModel(IFirewallService firewallService, IBlockedEntityRepository repository)
    {
        _firewallService = firewallService;
        _repository = repository;
        LoadBlockedEntities();
    }

    [RelayCommand]
    public async Task AddApplicationManual(object? parent)
    {
        if (parent is TopLevel topLevel)
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var patterns = isWindows ? new[] { "*.exe", "*.lnk" } : new[] { "*.app", "*" };
            var filterName = isWindows ? "Executables" : "Applications/Binaries";

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Application to Block",
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType(filterName) { Patterns = patterns } }
            });

            if (files.Count >= 1)
            {
                var path = files[0].Path.LocalPath;
                
                // Resolve shortcut if it's a .lnk file
                if (Path.GetExtension(path).Equals(".lnk", StringComparison.OrdinalIgnoreCase))
                {
                    path = ResolveShortcut(path);
                }

                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

                var name = Path.GetFileNameWithoutExtension(path);
                var id = $"NetBlocker_{ComputeHash(path)}";
                
                var entity = new BlockedEntity(id, path, name, BlockedEntityType.Application, true, DateTime.Now);
                
                if (await _firewallService.BlockEntityAsync(entity))
                {
                    await AddEntity(entity);
                }
            }
        }
    }

    [RelayCommand]
    public async Task ExportBlockedList(object? parent)
    {
        if (parent is TopLevel topLevel)
        {
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export Blocked Entities",
                DefaultExtension = ".json",
                FileTypeChoices = new[] { new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } } }
            });

            if (file != null)
            {
                var entities = await _repository.GetAllAsync();
                var json = JsonSerializer.Serialize(entities, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(file.Path.LocalPath, json);
            }
        }
    }

    [RelayCommand]
    public async Task ImportBlockedList(object? parent)
    {
        if (parent is TopLevel topLevel)
        {
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Import Blocked Entities",
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } } }
            });

            if (files.Count >= 1)
            {
                try
                {
                    var path = files[0].Path.LocalPath;
                    var json = await File.ReadAllTextAsync(path);
                    var entities = JsonSerializer.Deserialize<List<BlockedEntity>>(json);
                    
                    if (entities != null)
                    {
                        foreach (var entity in entities)
                        {
                            // Check if already exists? For now just try to add.
                            if (await _firewallService.BlockEntityAsync(entity))
                            {
                                await _repository.AddAsync(entity);
                            }
                        }
                        LoadBlockedEntities();
                    }
                }
                catch (Exception ex)
                {
                    // For now, silent fail or basic error handling
                    Debug.WriteLine($"Import failed: {ex.Message}");
                }
            }
        }
    }

    private string ResolveShortcut(string lnkPath)
    {
        try
        {
            // Simple approach for Windows using COM/WshShell if available, 
            // but since we want to avoid extra dependencies, let's use a 
            // basic P/Invoke or ShellLink parsing if needed.
            // For now, let's use the Shell COM wrapper which is standard on Windows.
            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return lnkPath;

            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shell.CreateShortcut(lnkPath);
            return (string)shortcut.TargetPath;
        }
        catch
        {
            return lnkPath;
        }
    }

    private string ComputeHash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).Substring(0, 12);
    }

    private async void LoadBlockedEntities()
    {
        var entities = await _repository.GetAllAsync();
        foreach (var entity in entities)
        {
            BlockedEntities.Add(entity);
            // Ensure rule is applied in the firewall service on startup
            if (entity.IsEnabled)
            {
                await _firewallService.BlockEntityAsync(entity);
            }
        }
    }

    public async Task AddEntity(BlockedEntity entity)
    {
        if (!BlockedEntities.Any(e => e.Id == entity.Id))
        {
            BlockedEntities.Add(entity);
            await _repository.AddAsync(entity);
        }
    }

    [RelayCommand]
    private async Task Unblock(BlockedEntity entity)
    {
        if (await _firewallService.UnblockEntityAsync(entity.Id))
        {
            await _repository.DeleteAsync(entity.Id);
            BlockedEntities.Remove(entity);
        }
    }
}
