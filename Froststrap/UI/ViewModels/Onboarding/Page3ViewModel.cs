using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Froststrap.UI.Elements.Onboarding;

namespace Froststrap.UI.ViewModels.Onboarding
{
    internal sealed class Page3ViewModel
    {
        public ICommand LaunchSettingsCommand => new RelayCommand(() => Finish(NextAction.LaunchSettings));
        public ICommand LaunchRobloxCommand => new RelayCommand(() => Finish(NextAction.LaunchRoblox));

        private static void Finish(NextAction action)
        {
            if (MainWindow.Instance is not { } window) return;
            window.CloseAction = action;
            window.Close();
        }
    }
}
