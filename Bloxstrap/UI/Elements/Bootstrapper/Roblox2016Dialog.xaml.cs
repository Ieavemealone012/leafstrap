using Bloxstrap.UI.Elements.Bootstrapper.Base;
using Bloxstrap.UI.ViewModels.Bootstrapper;
using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Shell;

namespace Bloxstrap.UI.Elements.Bootstrapper
{
    public partial class Roblox2016Dialog : IBootstrapperDialog
    {
        private readonly BootstrapperDialogViewModel _viewModel;
        private bool _isClosing;

        public Bloxstrap.Bootstrapper? Bootstrapper { get; set; }

        public string Message
        {
            get => _viewModel.Message;
            set { _viewModel.Message = value; _viewModel.OnPropertyChanged(nameof(_viewModel.Message)); }
        }

        public ProgressBarStyle ProgressStyle
        {
            get => _viewModel.ProgressIndeterminate ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
            set { _viewModel.ProgressIndeterminate = value == ProgressBarStyle.Marquee; _viewModel.OnPropertyChanged(nameof(_viewModel.ProgressIndeterminate)); }
        }

        public int ProgressMaximum
        {
            get => _viewModel.ProgressMaximum;
            set { _viewModel.ProgressMaximum = value; _viewModel.OnPropertyChanged(nameof(_viewModel.ProgressMaximum)); }
        }

        public int ProgressValue
        {
            get => _viewModel.ProgressValue;
            set { _viewModel.ProgressValue = value; _viewModel.OnPropertyChanged(nameof(_viewModel.ProgressValue)); }
        }

        public TaskbarItemProgressState TaskbarProgressState
        {
            get => _viewModel.TaskbarProgressState;
            set { _viewModel.TaskbarProgressState = value; _viewModel.OnPropertyChanged(nameof(_viewModel.TaskbarProgressState)); }
        }

        public double TaskbarProgressValue
        {
            get => _viewModel.TaskbarProgressValue;
            set { _viewModel.TaskbarProgressValue = value; _viewModel.OnPropertyChanged(nameof(_viewModel.TaskbarProgressValue)); }
        }

        public bool CancelEnabled
        {
            get => _viewModel.CancelEnabled;
            set { _viewModel.CancelEnabled = value; _viewModel.OnPropertyChanged(nameof(_viewModel.CancelEnabled)); }
        }

        public Roblox2016Dialog()
        {
            InitializeComponent();
            _viewModel = new BootstrapperDialogViewModel(this);
            DataContext = _viewModel;
            Title = App.Settings.Prop.BootstrapperTitle;
            Icon = App.Settings.Prop.BootstrapperIcon.GetIcon().GetImageSource();
        }

        private void UiWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!_isClosing)
                Bootstrapper?.Cancel();
        }

        public void ShowBootstrapper() => ShowDialog();

        public void CloseBootstrapper()
        {
            _isClosing = true;
            Dispatcher.BeginInvoke(Close);
        }

        public void ShowSuccess(string message, Action? callback) => BaseFunctions.ShowSuccess(message, callback);
    }
}
