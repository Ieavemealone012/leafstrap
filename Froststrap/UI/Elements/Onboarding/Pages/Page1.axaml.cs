using Avalonia.Controls;
using Froststrap.UI.ViewModels.Onboarding;

namespace Froststrap.UI.Elements.Onboarding.Pages
{
    internal partial class Page1 : UserControl
    {
        public Page1()
        {
            DataContext = new Page1ViewModel();
            InitializeComponent();
        }
    }
}
