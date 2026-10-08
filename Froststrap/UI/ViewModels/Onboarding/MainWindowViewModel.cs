using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Froststrap.UI.ViewModels.Onboarding
{
    internal class MainWindowViewModel : NotifyPropertyChangedViewModel
    {
        private string _nextButtonText = Strings.Common_Next;
        private bool _backButtonEnabled;
        private bool _nextButtonEnabled = true;

        public string NextButtonText { get => _nextButtonText; private set => SetProperty(ref _nextButtonText, value); }
        public bool BackButtonEnabled { get => _backButtonEnabled; set => SetProperty(ref _backButtonEnabled, value); }
        public bool NextButtonEnabled { get => _nextButtonEnabled; set => SetProperty(ref _nextButtonEnabled, value); }

        public ICommand BackPageCommand => new RelayCommand(() => PageRequest?.Invoke(this, "back"));
        public ICommand NextPageCommand => new RelayCommand(() => PageRequest?.Invoke(this, "next"));
        public ICommand CloseWindowCommand => new RelayCommand(() => CloseWindowRequest?.Invoke(this, EventArgs.Empty));

        public event EventHandler<string>? PageRequest;
        public event EventHandler? CloseWindowRequest;
        public void SetNextButtonText(string text) => NextButtonText = text;
    }
}
