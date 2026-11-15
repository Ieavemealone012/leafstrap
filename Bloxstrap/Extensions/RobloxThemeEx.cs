using Bloxstrap.Enums.AppStoragePresets;

namespace Bloxstrap.Extensions
{
    internal static class RobloxThemeEx
    {
        public static IReadOnlyCollection<RobloxTheme> Selections { get; } =
        [
            RobloxTheme.Default,
            RobloxTheme.Light,
            RobloxTheme.Dark
        ];
    }
}
