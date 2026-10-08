using Froststrap.UI.Elements.Base;
using Froststrap.UI.ViewModels.Dialogs;

namespace Froststrap.UI.Elements.Dialogs
{
    internal partial class LanguageSelectorDialog : AvaloniaWindow
    {
        public LanguageSelectorDialog()
        {
            var viewModel = new LanguageSelectorViewModel();
            DataContext = viewModel;
            InitializeComponent();
            viewModel.CloseRequestEvent += (_, _) =>
            {
                App.Settings.Save();
                Close();
            };
        }
    }
}
