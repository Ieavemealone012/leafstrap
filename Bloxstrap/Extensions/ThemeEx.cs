using Microsoft.Win32;

namespace Bloxstrap.Extensions
{
    public static class ThemeEx
    {
        public static bool IsSystemDark()
        {
            using var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }

        public static Theme GetFinal(this Theme dialogTheme)
        {
            if (dialogTheme != Theme.Default)
                return dialogTheme;

            if (IsSystemDark())
                return Theme.Dark;

            return Theme.Light;
        }
    }
}
