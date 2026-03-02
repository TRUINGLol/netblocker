using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InternetBlocker.Core.Interfaces;
using InternetBlocker.Core.Models;
using DynamicData;
using DynamicData.Binding;
using Avalonia.ReactiveUI;
using System.Runtime.InteropServices;

namespace InternetBlocker.ViewModels;

public partial class MonitorViewModel : ViewModelBase
{
    private readonly IMonitorService _monitorService;
    private readonly IFirewallService _firewallService;
    private readonly IBlockedEntityRepository _repository;
    private readonly IIconService _iconService;
    private readonly INotificationService _notificationService;
    private readonly Func<BlockedEntity, Task> _onEntityBlocked;
    private readonly SourceCache<ConnectionInfo, string> _connectionsCache = new(c => $"{c.Protocol}_{c.LocalAddress}:{c.LocalPort}_{c.RemoteAddress}:{c.RemotePort}");
    private readonly ReadOnlyObservableCollection<ConnectionInfo> _connections = null!;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte[]> _iconCache = new();
    private readonly System.Collections.Generic.HashSet<string> _seenProcesses = new();

    private bool _isScanning = true;
    public bool IsScanning
    {
        get => _isScanning;
        set => SetProperty(ref _isScanning, value);
    }

    public ReadOnlyObservableCollection<ConnectionInfo> Connections => _connections;
    
    private int _totalConnections;
    public int TotalConnections 
    {
        get => _totalConnections;
        set => SetProperty(ref _totalConnections, value);
    }

    private int _uniqueProcessesCount;
    public int UniqueProcessesCount
    {
        get => _uniqueProcessesCount;
        set => SetProperty(ref _uniqueProcessesCount, value);
    }

    public MonitorViewModel(IMonitorService monitorService, IFirewallService firewallService, IBlockedEntityRepository repository, IIconService iconService, INotificationService notificationService, Func<BlockedEntity, Task> onEntityBlocked)
    {
        _monitorService = monitorService;
        _firewallService = firewallService;
        _repository = repository;
        _iconService = iconService;
        _notificationService = notificationService;
        _onEntityBlocked = onEntityBlocked;

        // Bind the cache to the observable collection
        _connectionsCache.Connect()
            .Sort(SortExpressionComparer<ConnectionInfo>.Descending(c => c.ProcessName))
            .ObserveOn(AvaloniaScheduler.Instance)
            .Bind(out _connections)
            .Subscribe();

        StartPolling();
    }

    private readonly System.Collections.Generic.HashSet<string> _pendingIcons = new();

    private async void StartPolling()
    {
        while (_isScanning)
        {
            try
            {
                var currentConnections = await _monitorService.GetActiveConnectionsAsync();
                var connectionList = currentConnections.ToList();
                
                TotalConnections = connectionList.Count;
                
                // Use Edit to update the cache. DynamicData will handle additions, removals, and updates.
                _connectionsCache.Edit(innerList =>
                {
                    // Remove connections that are no longer active
                    var keysToRemove = innerList.Keys.Except(connectionList.Select(c => $"{c.Protocol}_{c.LocalAddress}:{c.LocalPort}_{c.RemoteAddress}:{c.RemotePort}")).ToList();
                    innerList.Remove(keysToRemove);

                    // Add or update current connections
                    var blockedEntities = _repository.GetAllAsync().GetAwaiter().GetResult().ToList();

                    foreach (var conn in connectionList)
                    {
                        // Reactive blocking for macOS (and backup for others)
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                        {
                            var isBlocked = blockedEntities.Any(e => 
                                e.Type == BlockedEntityType.Application && 
                                (e.Value == conn.ProcessPath || conn.ProcessPath.StartsWith(e.Value)));
                            
                            if (isBlocked)
                            {
                                // Call BlockEntityAsync with IP - MacFirewallService will add to PF table
                                _ = _firewallService.BlockEntityAsync(new BlockedEntity(
                                    $"IP_{conn.RemoteAddress}", conn.RemoteAddress, conn.RemoteAddress, 
                                    BlockedEntityType.IpAddress, true, DateTime.Now));
                            }
                        }

                        if (!_seenProcesses.Contains(conn.ProcessPath) && conn.ProcessPath != "Unknown")
                        {
                            _seenProcesses.Add(conn.ProcessPath);
                            // Only notify if we are already scanning (not on first run)
                            _ = _notificationService.ShowNotificationAsync("New Connection", $"{conn.ProcessName} is connecting to {conn.RemoteAddress}");
                        }

                        if (!_iconCache.TryGetValue(conn.ProcessPath, out var iconData))
                        {
                            // We will fetch it asynchronously to not block the polling loop.
                            lock (_pendingIcons)
                            {
                                if (_pendingIcons.Add(conn.ProcessPath))
                                {
                                    _ = FetchIconAsync(conn.ProcessPath);
                                }
                            }
                            innerList.AddOrUpdate(conn);
                        }
                        else
                        {
                            innerList.AddOrUpdate(conn with { IconData = iconData });
                        }
                    }
                    
                    UniqueProcessesCount = _seenProcesses.Count;
                });
            }
            catch { /* Log error */ }

            await Task.Delay(2000);
        }
    }

    private async Task FetchIconAsync(string path)
    {
        if (string.IsNullOrEmpty(path) || path == "Unknown") return;

        try
        {
            var data = await _iconService.GetIconAsync(path);
            if (data != null)
            {
                _iconCache.TryAdd(path, data);
                // Updating the cache will trigger a UI update for all connections with this path
                _connectionsCache.Edit(innerList =>
                {
                    var affected = innerList.Items.Where(c => c.ProcessPath == path).ToList();
                    foreach (var conn in affected)
                    {
                        innerList.AddOrUpdate(conn with { IconData = data });
                    }
                });
            }
        }
        finally
        {
            lock (_pendingIcons)
            {
                _pendingIcons.Remove(path);
            }
        }
    }

    [RelayCommand]
    private async Task BlockApp(ConnectionInfo connection)
    {
        if (string.IsNullOrEmpty(connection.ProcessPath) || connection.ProcessPath == "Unknown") return;

        var id = $"NetBlocker_{ComputeHash(connection.ProcessPath)}";
        var entity = new BlockedEntity(id, connection.ProcessPath, connection.ProcessName, BlockedEntityType.Application, true, DateTime.Now);

        if (await _firewallService.BlockEntityAsync(entity))
        {
            if (_onEntityBlocked != null)
            {
                await _onEntityBlocked(entity);
            }
        }
    }

    private string ComputeHash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).Substring(0, 12);
    }
}
