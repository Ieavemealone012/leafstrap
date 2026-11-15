using Bloxstrap.RobloxInterfaces;
using System.ComponentModel;
using System.Windows;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class ChannelViewModel : NotifyPropertyChangedViewModel
    {
        private static readonly Regex VersionHashRegex = new("^version-[0-9a-f]{16}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public ChannelViewModel()
        {
            Task.Run(() => LoadChannelDeployInfo(App.Settings.Prop.Channel));
        }

        public bool CheckForUpdates
        {
            get => App.Settings.Prop.CheckForUpdates;
            set => App.Settings.Prop.CheckForUpdates = value;
        }

        public bool CheckForPreRelease
        {
            get => App.Settings.Prop.CheckForPreRelease;
            set => App.Settings.Prop.CheckForPreRelease = value;
        }

        public bool TestModeEnabled
        {
            get => App.LaunchSettings.TestModeFlag.Active;
            set
            {
                if (value && !App.State.Prop.TestModeWarningShown)
                {
                    var result = Frontend.ShowMessageBox(Strings.Menu_TestMode_Prompt, MessageBoxImage.Information, MessageBoxButton.YesNo);
                    if (result != MessageBoxResult.Yes)
                    {
                        OnPropertyChanged(nameof(TestModeEnabled));
                        return;
                    }

                    App.State.Prop.TestModeWarningShown = true;
                }

                App.LaunchSettings.TestModeFlag.Active = value;
                OnPropertyChanged(nameof(TestModeEnabled));
            }
        }

        public bool PlayerVersionOverrideEnabled
        {
            get => App.Settings.Prop.PlayerVersionOverrideEnabled;
            set
            {
                App.Settings.Prop.PlayerVersionOverrideEnabled = value;
                OnPropertyChanged(nameof(PlayerVersionOverrideEnabled));
                OnPropertyChanged(nameof(PlayerVersionValidationMessage));
            }
        }

        public string PlayerVersionOverrideHash
        {
            get => App.Settings.Prop.PlayerVersionOverrideHash;
            set
            {
                App.Settings.Prop.PlayerVersionOverrideHash = value.Trim();
                OnPropertyChanged(nameof(PlayerVersionOverrideHash));
                OnPropertyChanged(nameof(PlayerVersionValidationMessage));
            }
        }

        public string PlayerVersionValidationMessage => GetVersionValidationMessage(PlayerVersionOverrideEnabled, PlayerVersionOverrideHash);

        public bool StudioVersionOverrideEnabled
        {
            get => App.Settings.Prop.StudioVersionOverrideEnabled;
            set
            {
                App.Settings.Prop.StudioVersionOverrideEnabled = value;
                OnPropertyChanged(nameof(StudioVersionOverrideEnabled));
                OnPropertyChanged(nameof(StudioVersionValidationMessage));
            }
        }

        public string StudioVersionOverrideHash
        {
            get => App.Settings.Prop.StudioVersionOverrideHash;
            set
            {
                App.Settings.Prop.StudioVersionOverrideHash = value.Trim();
                OnPropertyChanged(nameof(StudioVersionOverrideHash));
                OnPropertyChanged(nameof(StudioVersionValidationMessage));
            }
        }

        public string StudioVersionValidationMessage => GetVersionValidationMessage(StudioVersionOverrideEnabled, StudioVersionOverrideHash);

        private static string GetVersionValidationMessage(bool enabled, string versionHash)
        {
            if (!enabled)
                return "The latest available version will be used.";

            if (string.IsNullOrWhiteSpace(versionHash))
                return "Enter a version hash before launching.";

            return VersionHashRegex.IsMatch(versionHash)
                ? "Version hash format is valid. Leafstrap will verify it when Roblox launches."
                : "Expected format: version- followed by 16 hexadecimal characters.";
        }

        public bool IsRobloxInstallationMissing => !App.IsPlayerInstalled && !App.IsStudioInstalled;

        private async Task LoadChannelDeployInfo(string channel)
        {
            ShowLoadingError = false;
            OnPropertyChanged(nameof(ShowLoadingError));

            ChannelInfoLoadingText = Strings.Menu_Channel_Switcher_Fetching;
            OnPropertyChanged(nameof(ChannelInfoLoadingText));

            ChannelDeployInfo = null;
            OnPropertyChanged(nameof(ChannelDeployInfo));

            try
            {
                bool isPrivate = await Deployment.IsChannelPrivate(channel);
                if (App.Cookies.Loaded && isPrivate && string.IsNullOrEmpty(Deployment.ChannelToken))
                {
                    UserChannel? userChannel = await Deployment.GetUserChannel("WindowsPlayer");

                    if (userChannel?.Token is not null)
                        Deployment.ChannelToken = userChannel.Token;
                }

                ClientVersion info = await Deployment.GetInfo(channel, true, true);

                ShowChannelWarning = info.IsBehindDefaultChannel;
                OnPropertyChanged(nameof(ShowChannelWarning));

                ChannelDeployInfo = new DeployInfo
                {
                    Version = info.Version,
                    VersionGuid = isPrivate ? "version-private" : info.VersionGuid, // we dont want to return the hash of private channels for obvious reason
                    Timestamp = info.Timestamp?.ToLocalTime().ToString() ?? "?"
                };

                App.State.Prop.IgnoreOutdatedChannel = true;

                OnPropertyChanged(nameof(ChannelDeployInfo));
            }
            catch (InvalidChannelException ex)
            {
                ShowLoadingError = true;
                OnPropertyChanged(nameof(ShowLoadingError));

                if (ex.StatusCode == HttpStatusCode.Unauthorized)
                    ChannelInfoLoadingText = Strings.Menu_Channel_Switcher_Unauthorized;
                else
                    ChannelInfoLoadingText = $"An http error has occured ({ex.StatusCode})";

                OnPropertyChanged(nameof(ChannelInfoLoadingText));
            }
        }

        public bool ShowLoadingError { get; set; } = false;
        public bool ShowChannelWarning { get; set; } = false;

        public DeployInfo? ChannelDeployInfo { get; private set; } = null;
        public string ChannelInfoLoadingText { get; private set; } = null!;

        public string ViewChannel
        {
            get => App.Settings.Prop.Channel;
            set
            {
                value = value.Trim();
                Task.Run(() => LoadChannelDeployInfo(value));

                if (value.ToLower() == "live" || value.ToLower() == "zlive")
                {
                    App.Settings.Prop.Channel = Deployment.DefaultChannel;
                }
                else
                {
                    App.Settings.Prop.Channel = value;
                }
            }
        }

        public bool UpdateRoblox
        {
            get => App.Settings.Prop.UpdateRoblox && !IsRobloxInstallationMissing;
            set => App.Settings.Prop.UpdateRoblox = value;
        }

        public double MaxThreadDownload
        {
            get => App.Settings.Prop.MaxThreadDownload;
            set => App.Settings.Prop.MaxThreadDownload = Math.Clamp((int)value, 1, 8);
        }

        public bool StaticDirectory
        {
            get => App.Settings.Prop.StaticDirectory;
            set => App.Settings.Prop.StaticDirectory = value;
        }

        public IReadOnlyDictionary<string, DistributorType> ClientDistributions { get; } = new Dictionary<string, DistributorType>
        {
            { "Global (Default)", DistributorType.Global },
            { "VNG (Việt Nam)", DistributorType.VNGGames }
        };

        public string SelectedDistribution
        {
            get => ClientDistributions.First(x => x.Value == App.Settings.Prop.DistributorType).Key;
            set
            {
                if (!ClientDistributions.TryGetValue(value, out DistributorType type))
                    return;

                App.Settings.Prop.DistributorType = type;
                App.Settings.Prop.RobloxDomain = type == DistributorType.VNGGames
                    ? "robloxapp.vnggames.com"
                    : Deployment.DefaultRobloxDomain;
                OnPropertyChanged(nameof(RobloxDomain));
            }
        }

        public string RobloxDomain
        {
            get => App.Settings.Prop.RobloxDomain;
            set
            {
                string domain = value.Trim().TrimEnd('/');
                if (!string.IsNullOrWhiteSpace(domain) &&
                    !domain.Contains("://", StringComparison.Ordinal) &&
                    !domain.Any(char.IsWhiteSpace))
                    App.Settings.Prop.RobloxDomain = domain;
                OnPropertyChanged(nameof(RobloxDomain));
            }
        }

        public IReadOnlyDictionary<string, ChannelChangeMode> ChannelChangeModes => new Dictionary<string, ChannelChangeMode>
        {
            { Strings.Menu_Channel_ChangeAction_Automatic, ChannelChangeMode.Automatic },
            { Strings.Menu_Channel_ChangeAction_Prompt, ChannelChangeMode.Prompt },
            { Strings.Menu_Channel_ChangeAction_Ignore, ChannelChangeMode.Ignore },
        };

        public string SelectedChannelChangeMode
        {
            get => ChannelChangeModes.FirstOrDefault(x => x.Value == App.Settings.Prop.ChannelChangeMode).Key;
            set => App.Settings.Prop.ChannelChangeMode = ChannelChangeModes[value];
        }

        public bool ForceRobloxReinstallation
        {
            get => App.State.Prop.ForceReinstall || IsRobloxInstallationMissing;
            set => App.State.Prop.ForceReinstall = value;
        }
    }
}
