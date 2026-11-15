using Bloxstrap.UI.Elements.Settings.Pages;
using Bloxstrap.UI.ViewModels.Settings;
using Bloxstrap.UI.Elements.Controls;
using Bloxstrap.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;
using Wpf.Ui.Controls.Interfaces;
using Wpf.Ui.Mvvm.Contracts;

namespace Bloxstrap.UI.Elements.Settings
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INavigationWindow
    {
        private Models.Persistable.WindowState _state => App.State.Prop.SettingsWindow;
        private readonly Random _halloweenRandom = new();
        private DispatcherTimer? _halloweenTimer;
        private List<SearchBarItem> _searchIndex = new();

        public MainWindow(bool showAlreadyRunningWarning)
        {
            var viewModel = new MainWindowViewModel();

            viewModel.RequestSaveNoticeEvent += (_, _) => SettingsSavedSnackbar.Show();
            viewModel.RequestCloseWindowEvent += (_, _) => Close();

            DataContext = viewModel;

            InitializeComponent();

            Loaded += (_, _) =>
            {
                UpdateHalloweenOverlay();
                Dispatcher.InvokeAsync(BuildSearchIndex, DispatcherPriority.Background);
            };

            App.Logger.WriteLine("MainWindow", "Initializing settings window");

            if (showAlreadyRunningWarning)
                ShowAlreadyRunningSnackbar();

            gbs.Opacity = viewModel.GBSEnabled ? 1 : 0.5;
            gbs.IsEnabled = viewModel.GBSEnabled; // binding doesnt work as expected so we are setting it in here instead

            LoadState();

            string? lastPageName = App.State.Prop.LastPage;
            Type? lastPage = lastPageName is null ? null : Type.GetType(lastPageName);

            App.WindowsBackdrop();

            if (lastPage != null)
                SafeNavigate(lastPage);
            else
                RootNavigation.SelectedPageIndex = 0;

            RootNavigation.Navigated += OnNavigation!;

            void OnNavigation(object? sender, RoutedNavigationEventArgs e)
            {
                INavigationItem? currentPage = RootNavigation.Current;
                App.State.Prop.LastPage = currentPage?.PageType.FullName!;
            }
        }

        private async void SafeNavigate(Type page)
        {
            await Task.Delay(500); // ensure page service is ready

            if (page == typeof(RobloxSettingsPage) && !App.GlobalSettings.Loaded)
                return; // prevent from navigating onto disabled page

            Navigate(page);
        }

        public void LoadState()
        {
            if (_state.Left > SystemParameters.VirtualScreenWidth)
                _state.Left = 0;

            if (_state.Top > SystemParameters.VirtualScreenHeight)
                _state.Top = 0;

            if (_state.Width > 0)
                this.Width = _state.Width;

            if (_state.Height > 0)
                this.Height = _state.Height;

            if (_state.Left > 0 && _state.Top > 0)
            {
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                this.Left = _state.Left;
                this.Top = _state.Top;
            }
        }

        private async void ShowAlreadyRunningSnackbar()
        {
            await Task.Delay(500); // wait for everything to finish loading
            AlreadyRunningSnackbar.Show();
        }

        private void BuildSearchIndex()
        {
            var index = new List<SearchBarItem>();

            foreach (var item in RootNavigation.Items.OfType<NavigationItem>())
            {
                if (item.PageType is null || item.Visibility != Visibility.Visible)
                    continue;

                if (Activator.CreateInstance(item.PageType) is not Page page)
                    continue;

                if (item.Content is string pageName && !string.IsNullOrWhiteSpace(pageName))
                    index.Add(new SearchBarItem { DisplayName = pageName, PageType = item.PageType });

                foreach (var option in FindLogicalChildren<OptionControl>(page))
                {
                    if (!string.IsNullOrWhiteSpace(option.Header))
                        index.Add(new SearchBarItem { DisplayName = option.Header, PageType = item.PageType });
                }
            }

            _searchIndex = index
                .GroupBy(item => (item.DisplayName, item.PageType))
                .Select(group => group.First())
                .OrderBy(item => item.DisplayName)
                .ToList();
        }

        private static IEnumerable<T> FindLogicalChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            foreach (object rawChild in LogicalTreeHelper.GetChildren(parent))
            {
                if (rawChild is not DependencyObject child)
                    continue;

                if (child is T match)
                    yield return match;

                foreach (T descendant in FindLogicalChildren<T>(child))
                    yield return descendant;
            }
        }

        private void SettingsSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not AutoSuggestBox searchBox)
                return;

            string query = searchBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                searchBox.ItemsSource = null;
                return;
            }

            SearchBarItem? exact = _searchIndex.FirstOrDefault(item =>
                item.DisplayName.Equals(query, StringComparison.OrdinalIgnoreCase));

            if (exact is not null)
            {
                Navigate(exact.PageType);
                searchBox.ItemsSource = null;
                return;
            }

            searchBox.ItemsSource = _searchIndex
                .Where(item => item.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(12)
                .Select(item => item.DisplayName)
                .ToList();
        }

        #region INavigationWindow methods

        public Frame GetFrame() => RootFrame;

        public INavigation GetNavigation() => RootNavigation;

        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        public void SetPageService(IPageService pageService) => RootNavigation.PageService = pageService;

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        #endregion INavigationWindow methods

        private void WpfUiWindow_Closing(object sender, CancelEventArgs e)
        {
            if (App.FastFlags.Changed || App.PendingSettingTasks.Any())
            {
                var result = Frontend.ShowMessageBox(Strings.Menu_UnsavedChanges, MessageBoxImage.Warning, MessageBoxButton.YesNo);

                if (result != MessageBoxResult.Yes)
                    e.Cancel = true;
            }

            _state.Width = this.Width;
            _state.Height = this.Height;

            _state.Top = this.Top;
            _state.Left = this.Left;

            App.State.Save();
        }

        private void WpfUiWindow_Closed(object sender, EventArgs e)
        {
            _halloweenTimer?.Stop();

            if (App.LaunchSettings.TestModeFlag.Active)
                LaunchHandler.LaunchRoblox(LaunchMode.Player);
            else
                App.SoftTerminate();
        }

        public new void ApplyTheme()
        {
            base.ApplyTheme();

            if (IsInitialized)
                UpdateHalloweenOverlay();
        }

        private void UpdateHalloweenOverlay()
        {
            bool enabled = App.Settings.Prop.Theme.GetFinal() == Enums.Theme.Halloween;
            HalloweenOverlay.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;

            if (!enabled)
            {
                _halloweenTimer?.Stop();
                HalloweenOverlay.Children.Clear();
                return;
            }

            if (!IsLoaded || ActualWidth <= 0 || ActualHeight <= 0)
                return;

            _halloweenTimer ??= new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1350)
            };

            _halloweenTimer.Tick -= HalloweenTimer_Tick;
            _halloweenTimer.Tick += HalloweenTimer_Tick;
            _halloweenTimer.Start();

            if (HalloweenOverlay.Children.Count == 0)
            {
                for (int i = 0; i < 4; i++)
                    SpawnHalloweenVector(initialPlacement: true);
            }
        }

        private void HalloweenTimer_Tick(object? sender, EventArgs e)
        {
            if (HalloweenOverlay.Children.Count < 8)
                SpawnHalloweenVector(initialPlacement: false);
        }

        private void SpawnHalloweenVector(bool initialPlacement)
        {
            bool isGhost = _halloweenRandom.NextDouble() < 0.32;
            FrameworkElement vector = isGhost ? CreateGhostVector() : CreateBatVector();

            double scale = isGhost
                ? 34 + _halloweenRandom.NextDouble() * 30
                : 30 + _halloweenRandom.NextDouble() * 46;

            vector.Width = scale;
            vector.Height = isGhost ? scale * 1.08 : scale * 0.58;
            vector.Opacity = isGhost
                ? 0.13 + _halloweenRandom.NextDouble() * 0.12
                : 0.15 + _halloweenRandom.NextDouble() * 0.18;
            vector.RenderTransformOrigin = new Point(0.5, 0.5);

            bool leftToRight = _halloweenRandom.Next(2) == 0;
            var rotate = new RotateTransform(_halloweenRandom.Next(-12, 13));
            var transforms = new TransformGroup();
            transforms.Children.Add(new ScaleTransform(leftToRight ? 1 : -1, 1));
            transforms.Children.Add(rotate);
            vector.RenderTransform = transforms;

            double startX = leftToRight ? -scale - 12 : ActualWidth + scale + 12;
            double endX = leftToRight ? ActualWidth + scale + 12 : -scale - 12;
            double maxY = Math.Max(90, ActualHeight - vector.Height - 60);
            double y = 48 + _halloweenRandom.NextDouble() * (maxY - 48);

            if (initialPlacement)
            {
                double progress = 0.14 + _halloweenRandom.NextDouble() * 0.66;
                startX = leftToRight
                    ? -scale + (ActualWidth + scale * 2) * progress
                    : ActualWidth + scale - (ActualWidth + scale * 2) * progress;
            }

            Canvas.SetLeft(vector, startX);
            Canvas.SetTop(vector, y);
            HalloweenOverlay.Children.Add(vector);

            var travel = new DoubleAnimation(startX, endX, TimeSpan.FromSeconds(10 + _halloweenRandom.NextDouble() * 9));
            travel.Completed += (_, _) => HalloweenOverlay.Children.Remove(vector);
            vector.BeginAnimation(Canvas.LeftProperty, travel);

            vector.BeginAnimation(
                Canvas.TopProperty,
                new DoubleAnimation(y, y + _halloweenRandom.Next(-28, 29), TimeSpan.FromSeconds(2.2 + _halloweenRandom.NextDouble() * 2.4))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                });

            rotate.BeginAnimation(
                RotateTransform.AngleProperty,
                new DoubleAnimation(rotate.Angle - 7, rotate.Angle + 7, TimeSpan.FromSeconds(1.8 + _halloweenRandom.NextDouble() * 1.8))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                });
        }

        private static FrameworkElement CreateBatVector()
        {
            return new System.Windows.Shapes.Path
            {
                Stretch = Stretch.Uniform,
                Fill = new SolidColorBrush(Color.FromRgb(20, 8, 28)),
                Stroke = new SolidColorBrush(Color.FromRgb(255, 122, 0)),
                StrokeThickness = 0.7,
                Data = Geometry.Parse("M2,18 C10,7 18,8 25,14 C27,7 30,4 32,11 C34,4 37,7 39,14 C46,8 54,7 62,18 C53,16 48,21 44,27 C39,23 35,22 32,27 C29,22 25,23 20,27 C16,21 11,16 2,18 Z")
            };
        }

        private static FrameworkElement CreateGhostVector()
        {
            var ghost = new Grid();
            ghost.Children.Add(new System.Windows.Shapes.Path
            {
                Stretch = Stretch.Fill,
                Fill = new SolidColorBrush(Color.FromRgb(255, 241, 218)),
                Stroke = new SolidColorBrush(Color.FromRgb(255, 154, 60)),
                StrokeThickness = 0.8,
                Data = Geometry.Parse("M8,38 V20 C8,9 15,2 24,2 C33,2 40,9 40,20 V38 L34,32 L29,38 L24,32 L19,38 L14,32 Z")
            });

            ghost.Children.Add(new Ellipse
            {
                Width = 5,
                Height = 7,
                Margin = new Thickness(0, 0, 12, 5),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Fill = new SolidColorBrush(Color.FromRgb(48, 24, 56))
            });
            ghost.Children.Add(new Ellipse
            {
                Width = 5,
                Height = 7,
                Margin = new Thickness(12, 0, 0, 5),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Fill = new SolidColorBrush(Color.FromRgb(48, 24, 56))
            });

            return ghost;
        }

        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            var menu = LaunchButton.ContextMenu;
            menu.PlacementTarget = LaunchButton;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Center;
            menu.VerticalOffset = 50;
            menu.IsOpen = true;
        }

        public void ShowLoading(string message = "Loading...")
        {
            LoadingOverlayText.Text = message;
            LoadingOverlay.Visibility = Visibility.Visible;
        }

        public void HideLoading()
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }
    }
}
