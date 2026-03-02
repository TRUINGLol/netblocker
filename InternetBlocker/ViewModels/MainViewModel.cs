using CommunityToolkit.Mvvm.ComponentModel;
using InternetBlocker.Core.Interfaces;

namespace InternetBlocker.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private ViewModelBase? _currentPage;
    public ViewModelBase? CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }

    private readonly IFirewallService _firewallService;
    private readonly IAutostartService _autostartService;
    public bool IsElevated => _firewallService.IsElevated;

    public bool IsAutostartEnabled
    {
        get => _autostartService.IsEnabled;
        set
        {
            if (value) _autostartService.Enable();
            else _autostartService.Disable();
            OnPropertyChanged(nameof(IsAutostartEnabled));
        }
    }

    public MonitorViewModel Monitor { get; }
    public BlockedViewModel Blocked { get; }
    public StatisticsViewModel Statistics { get; }

    public MainViewModel(IMonitorService monitorService, IFirewallService firewallService, IBlockedEntityRepository repository, IAutostartService autostartService, IIconService iconService, INotificationService notificationService)
    {
        _firewallService = firewallService;
        _autostartService = autostartService;
        Blocked = new BlockedViewModel(firewallService, repository);
        Monitor = new MonitorViewModel(monitorService, firewallService, repository, iconService, notificationService, async entity => await Blocked.AddEntity(entity));
        Statistics = new StatisticsViewModel(monitorService, repository);

        CurrentPage = Monitor;
    }

    public void NavigateToMonitor() => CurrentPage = Monitor;
    public void NavigateToBlocked() => CurrentPage = Blocked;
    public void NavigateToStatistics() => CurrentPage = Statistics;
}
