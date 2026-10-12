using System.Windows.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using Froststrap.UI.Elements.Onboarding;
using Froststrap.Utility;

namespace Froststrap.UI.ViewModels.Onboarding
{
    internal sealed class Page2ViewModel : NotifyPropertyChangedViewModel
    {
        private readonly string? _portableLinuxSource = ResolvePortableLinuxSource();
        private readonly bool _portableLinuxSourceIsAppImage = HasAppImageSource();
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
        public bool CanChangeLocation => OperatingSystem.IsLinux() && !string.IsNullOrWhiteSpace(_portableLinuxSource);
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
                if (CanChangeLocation && _portableLinuxSource is not null)
                {
                    string? directory = Path.GetDirectoryName(InstallLocation);
                    if (string.IsNullOrWhiteSpace(directory)) throw new IOException("Choose a valid installation location.");
                    Directory.CreateDirectory(directory);
                    if (!File.Exists(_portableLinuxSource))
                        throw new FileNotFoundException("The running Leafstrap package could not be found. Download the AppImage again and retry.", _portableLinuxSource);
                    if (!string.Equals(Path.GetFullPath(_portableLinuxSource), Path.GetFullPath(InstallLocation), StringComparison.Ordinal))
                        File.Copy(_portableLinuxSource, InstallLocation, true);
                    File.SetUnixFileMode(InstallLocation, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
                    executable = InstallLocation;
                }

                string icon = Shortcut.GetFroststrapIconPath();
                if (CreateDesktopShortcut)
                {
                    if (!Shortcut.Create(executable, string.Empty, Path.Combine(Paths.Desktop, "Leafstrap"), icon, integrateWithLinuxMenu: false))
                        throw new IOException("Leafstrap was copied, but its desktop shortcut could not be created.");
                }

                if (CreateMenuShortcut && OperatingSystem.IsLinux())
                {
                    if (!Shortcut.Create(executable, string.Empty, Path.Combine(Paths.UserProfile, ".local", "share", "applications", "Leafstrap"), icon, integrateWithLinuxMenu: false))
                        throw new IOException("Leafstrap was copied, but it could not be registered in the applications menu.");
                }

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
                InstallLocation = Path.Combine(path, _portableLinuxSourceIsAppImage ? "Leafstrap.AppImage" : "Leafstrap");
        }

        private string GetDefaultInstallLocation()
        {
            if (OperatingSystem.IsLinux() && !string.IsNullOrWhiteSpace(_portableLinuxSource))
                return Path.Combine(Paths.UserProfile, ".local", "bin", _portableLinuxSourceIsAppImage ? "Leafstrap.AppImage" : "Leafstrap");
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

        private static bool HasAppImageSource() =>
            GetExistingEnvironmentPath("LEAFSTRAP_APPIMAGE") is not null ||
            GetExistingEnvironmentPath("APPIMAGE") is not null;

        private static string? ResolvePortableLinuxSource()
        {
            if (!OperatingSystem.IsLinux()) return null;

            // AppImage normally exposes APPIMAGE. AppRun also mirrors it to a
            // Leafstrap-owned variable so desktop environments cannot lose it.
            string? appImage = GetExistingEnvironmentPath("LEAFSTRAP_APPIMAGE") ??
                               GetExistingEnvironmentPath("APPIMAGE");
            if (appImage is not null) return appImage;

            // Some AppImage launchers do not preserve APPIMAGE. The published
            // executable is self-contained, so copying it out of the temporary
            // mount is a safe fallback and prevents /tmp/.mount_* shortcuts.
            string? appDir = Environment.GetEnvironmentVariable("APPDIR");
            bool isTemporaryAppImage = !string.IsNullOrWhiteSpace(appDir) ||
                                       Paths.Process.Contains("/.mount_", StringComparison.Ordinal);
            return isTemporaryAppImage && File.Exists(Paths.Process) ? Paths.Process : null;
        }

        private static string? GetExistingEnvironmentPath(string variable)
        {
            string? value = Environment.GetEnvironmentVariable(variable);
            if (string.IsNullOrWhiteSpace(value)) return null;
            try
            {
                string fullPath = Path.GetFullPath(value);
                return File.Exists(fullPath) ? fullPath : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
