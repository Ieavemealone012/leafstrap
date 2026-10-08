// SPDX-FileCopyrightText: 2026 Froststrap
//
// SPDX-License-Identifier: MPL-2.0

using Avalonia;
using Avalonia.Platform;

namespace Froststrap.Extensions
{
    internal static class ThemeEx
    {
        public static Theme GetFinal(this Theme dialogTheme)
        {
            if (dialogTheme != Theme.Default)
                return dialogTheme;

            return IsSystemDark() ? Theme.Dark : Theme.Light;
        }

        public static bool IsSystemDark()
        {
            var variant = Application.Current?.PlatformSettings?.GetColorValues().ThemeVariant;
            return variant == PlatformThemeVariant.Dark;
        }
    }
}
