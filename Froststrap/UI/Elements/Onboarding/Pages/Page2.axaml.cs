using Avalonia.Controls;
using Avalonia.Interactivity;
using Froststrap.UI.ViewModels.Onboarding;

namespace Froststrap.UI.Elements.Onboarding.Pages
{
    internal partial class Page2 : UserControl
    {
        private readonly Page2ViewModel _viewModel = new();

        public Page2()
        {
            DataContext = _viewModel;
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (MainWindow.Instance is { } window)
                window.NextPageCallback = _viewModel.DoInstallAsync;
        }
    }
}
