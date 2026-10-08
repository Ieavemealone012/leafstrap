using System.Windows.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using Froststrap.UI.Elements.Onboarding;
using Froststrap.Utility;

namespace Froststrap.UI.ViewModels.Onboarding
{
    internal sealed class Page2ViewModel : NotifyPropertyChangedViewModel
    {
        private readonly string? _appImageSource = Environment.GetEnvironmentVariable("APPIMAGE");
        private string _installLocation;
        private string _errorMessage = string.Empty;
        private bool _createDesktopShortcut = true;
        private bool _createMenuShortcut = true;

        public Page2ViewModel()
        {
            _installLocation = GetDefaultInstallLocation();
            BrowseInstallLocationCommand = new AsyncRelayCommand(BrowseInstallLocationAsync);
            ResetInstallLocationCommand = new RelayCommand(() => InstallLocation = GetDefaultInstallLocation());
        }

        public string InstallLocation { get => _installLocation; set => SetProperty(ref _installLocation, value); }
        public bool CanChangeLocation => OperatingSystem.IsLinux() && !string.IsNullOrWhiteSpace(_appImageSource);
        public bool ShowPortableNotice => CanChangeLocation;
        public bool CreateDesktopShortcut { get => _createDesktopShortcut; set => SetProperty(ref _createDesktopShortcut, value); }
        public bool CreateMenuShortcut { get => _createMenuShortcut; set => SetProperty(ref _createMenuShortcut, value); }
        public string ErrorMessage { get => _errorMessage; private set { if (SetProperty(ref _errorMessage, value)) OnPropertyChanged(nameof(HasError)); } }
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
        public ICommand BrowseInstallLocationCommand { get; }
        public ICommand ResetInstallLocationCommand { get; }

        public async Task<bool> DoInstallAsync()
        {
            try
            {
                string executable = Paths.Process;
                if (CanChangeLocation && _appImageSource is not null)
                {
                    string? directory = Path.GetDirectoryName(InstallLocation);
                    if (string.IsNullOrWhiteSpace(directory)) throw new IOException("Choose a valid installation location.");
                    Directory.CreateDirectory(directory);
                    if (!string.Equals(Path.GetFullPath(_appImageSource), Path.GetFullPath(InstallLocation), StringComparison.Ordinal))
                        File.Copy(_appImageSource, InstallLocation, true);
                    File.SetUnixFileMode(InstallLocation, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
                    executable = InstallLocation;
                }

                string icon = Shortcut.GetFroststrapIconPath();
                if (CreateDesktopShortcut)
                    Shortcut.Create(executable, string.Empty, Path.Combine(Paths.Desktop, "Leafstrap"), icon, integrateWithLinuxMenu: false);

                if (CreateMenuShortcut && OperatingSystem.IsLinux())
                    Shortcut.Create(executable, string.Empty, Path.Combine(Paths.UserProfile, ".local", "share", "applications", "Leafstrap"), icon, integrateWithLinuxMenu: false);

                App.Settings.Save();
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Installation failed");
                ErrorMessage = ex.Message;
                return false;
            }
        }

        private async Task BrowseInstallLocationAsync()
        {
            if (MainWindow.Instance?.StorageProvider is not { } storage) return;
            var folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Choose Leafstrap installation folder", AllowMultiple = false });
            if (folders.Count > 0 && folders[0].TryGetLocalPath() is string path)
                InstallLocation = Path.Combine(path, "Leafstrap.AppImage");
        }

        private string GetDefaultInstallLocation()
        {
            if (OperatingSystem.IsLinux() && !string.IsNullOrWhiteSpace(_appImageSource))
                return Path.Combine(Paths.UserProfile, ".local", "bin", "Leafstrap.AppImage");
            if (OperatingSystem.IsMacOS())
                return ResolveMacBundlePath();
            return Paths.Process;
        }

        private static string ResolveMacBundlePath()
        {
            var directory = new FileInfo(Paths.Process).Directory;
            while (directory is not null)
            {
                if (directory.Name.EndsWith(".app", StringComparison.OrdinalIgnoreCase)) return directory.FullName;
                directory = directory.Parent;
            }
            return Paths.Process;
        }
    }
}
