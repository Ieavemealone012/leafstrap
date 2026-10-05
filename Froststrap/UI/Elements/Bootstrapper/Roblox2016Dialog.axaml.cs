// SPDX-FileCopyrightText: 2026 Leafstrap contributors
// SPDX-License-Identifier: MPL-2.0

using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Froststrap.UI.Elements.Bootstrapper.Base;
using Froststrap.UI.ViewModels.Bootstrapper;

namespace Froststrap.UI.Elements.Bootstrapper
{
    internal partial class Roblox2016Dialog : AvaloniaDialogBase
    {
        private readonly BootstrapperDialogViewModel _viewModel;

        public Roblox2016Dialog()
        {
            InitializeComponent();
            _viewModel = new BootstrapperDialogViewModel(this);
            DataContext = _viewModel;
            SetupDialog();

            var iconImage = App.Settings.Prop.BootstrapperIcon.GetIcon().GetImageSource();
            if (iconImage is Bitmap bitmap)
                Icon = new WindowIcon(bitmap);
        }

        public override string Message
        {
            get => _viewModel.Message;
            set => RunOnUI(() =>
            {
                _viewModel.Message = value;
                _viewModel.OnPropertyChanged(nameof(_viewModel.Message));
            });
        }

        public override int ProgressMaximum
        {
            get => _viewModel.ProgressMaximum;
            set => RunOnUI(() =>
            {
                _viewModel.ProgressMaximum = value;
                _viewModel.OnPropertyChanged(nameof(_viewModel.ProgressMaximum));
            });
        }

        public override int ProgressValue
        {
            get => _viewModel.ProgressValue;
            set => RunOnUI(() =>
            {
                _viewModel.ProgressValue = value;
                _viewModel.OnPropertyChanged(nameof(_viewModel.ProgressValue));
            });
        }

        public override bool CancelEnabled
        {
            get => _viewModel.CancelEnabled;
            set => RunOnUI(() =>
            {
                _viewModel.CancelEnabled = value;
                _viewModel.OnPropertyChanged(nameof(_viewModel.CancelEnabled));
            });
        }

        public override bool ProgressIndeterminate
        {
            get => _viewModel.ProgressIndeterminate;
            set => RunOnUI(() =>
            {
                _viewModel.ProgressIndeterminate = value;
                _viewModel.OnPropertyChanged(nameof(_viewModel.ProgressIndeterminate));
            });
        }
    }
}
