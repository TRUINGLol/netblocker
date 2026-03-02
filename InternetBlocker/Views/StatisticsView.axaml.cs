using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace InternetBlocker.Views;

public partial class StatisticsView : UserControl
{
    public StatisticsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
