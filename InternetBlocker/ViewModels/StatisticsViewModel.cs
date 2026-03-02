using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using InternetBlocker.Core.Interfaces;
using InternetBlocker.Core.Models;
using DynamicData;
using Avalonia.ReactiveUI;

namespace InternetBlocker.ViewModels;

public partial class StatisticsViewModel : ViewModelBase
{
    private readonly IMonitorService _monitorService;
    private readonly IBlockedEntityRepository _repository;

    [ObservableProperty]
    private int _totalConnectionsProcessed;

    [ObservableProperty]
    private int _activeConnectionsCount;

    [ObservableProperty]
    private int _blockedAppsCount;

    [ObservableProperty]
    private string _mostActiveApp = "None";

    private readonly Dictionary<string, int> _appConnectionCounts = new();

    public StatisticsViewModel(IMonitorService monitorService, IBlockedEntityRepository repository)
    {
        _monitorService = monitorService;
        _repository = repository;
        
        StartTracking();
    }

    private async void StartTracking()
    {
        while (true)
        {
            try
            {
                var connections = await _monitorService.GetActiveConnectionsAsync();
                var connectionList = connections.ToList();
                
                ActiveConnectionsCount = connectionList.Count;
                TotalConnectionsProcessed += connectionList.Count;

                foreach (var conn in connectionList)
                {
                    if (conn.ProcessPath != "Unknown")
                    {
                        _appConnectionCounts[conn.ProcessName] = _appConnectionCounts.GetValueOrDefault(conn.ProcessName) + 1;
                    }
                }

                if (_appConnectionCounts.Any())
                {
                    MostActiveApp = _appConnectionCounts.OrderByDescending(x => x.Value).First().Key;
                }

                var blocked = await _repository.GetAllAsync();
                BlockedAppsCount = blocked.Count(e => e.Type == BlockedEntityType.Application);
            }
            catch { }

            await Task.Delay(5000); // Statistics update every 5 seconds
        }
    }
}
