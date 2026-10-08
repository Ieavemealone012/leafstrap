using Avalonia.Controls;
using Avalonia.Media;
using Froststrap.UI.Elements.Base;
using Froststrap.UI.Elements.Onboarding.Pages;
using Froststrap.UI.Utility;
using Froststrap.UI.ViewModels.Onboarding;

namespace Froststrap.UI.Elements.Onboarding
{
    internal partial class MainWindow : AvaloniaWindow
    {
        public static MainWindow? Instance { get; private set; }
        internal readonly MainWindowViewModel _viewModel = new();
        private Type _currentPage = typeof(Page1);
        private bool _isInitialLoad = true;
        private readonly List<Type> _pages = [typeof(Page1), typeof(Page2), typeof(Page3)];

        public Func<Task<bool>>? NextPageCallback;
        public NextAction CloseAction = NextAction.Terminate;
        public bool Finished => _currentPage == _pages.Last();

        public MainWindow()
        {
            Instance = this;
            DataContext = _viewModel;
            InitializeComponent();

            _viewModel.PageRequest += async (_, type) =>
            {
                if (type == "next") await NextPage();
                else if (type == "back") BackPage();
            };
            _viewModel.CloseWindowRequest += (_, _) => Close();

            Navigate(typeof(Page1));
            App.Logger.Debug("Initializing Leafstrap installer window");
        }

        private async Task NextPage()
        {
            if (NextPageCallback is not null && !await NextPageCallback()) return;
            if (_currentPage == _pages.Last()) return;
            App.Settings.Save();
            Navigate(_pages[_pages.IndexOf(_currentPage) + 1]);
        }

        private void BackPage()
        {
            if (_currentPage == _pages.First()) return;
            Navigate(_pages[_pages.IndexOf(_currentPage) - 1]);
        }

        public void SetNextButtonEnabled(bool enabled) => _viewModel.NextButtonEnabled = enabled;

        public bool Navigate(Type pageType)
        {
            int currentIndex = _pages.IndexOf(_currentPage);
            int newIndex = _pages.IndexOf(pageType);
            RootFrame.PageTransition = _isInitialLoad ? null : new FluentSlideTransition
            {
                Direction = newIndex > currentIndex ? SlideDirection.Right : SlideDirection.Left,
                HorizontalOffset = 80,
                Duration = TimeSpan.FromMilliseconds(180)
            };
            _isInitialLoad = false;

            _currentPage = pageType;
            NextPageCallback = null;
            RootFrame.Content = Activator.CreateInstance(pageType);

            int index = _pages.IndexOf(pageType);
            PageTitle.Text = index switch { 0 => "Welcome", 1 => "Install", _ => "Completion" };
            WelcomeMarker.IsVisible = index == 0;
            InstallMarker.IsVisible = index == 1;
            CompletionMarker.IsVisible = index == 2;
            WelcomeNav.Background = index == 0 ? GetSelectedBrush() : Brushes.Transparent;
            InstallNav.Background = index == 1 ? GetSelectedBrush() : Brushes.Transparent;
            CompletionNav.Background = index == 2 ? GetSelectedBrush() : Brushes.Transparent;

            _viewModel.BackButtonEnabled = index > 0 && index < 2;
            _viewModel.NextButtonEnabled = index < 2;
            _viewModel.SetNextButtonText(index == 1 ? Strings.Common_Install : Strings.Common_Next);
            return true;
        }

        private static IBrush GetSelectedBrush()
        {
            if (Avalonia.Application.Current?.TryGetResource("SubtleFillColorSecondaryBrush", null, out var value) == true && value is IBrush brush)
                return brush;
            return Brushes.Transparent;
        }
    }
}
