// SPDX-FileCopyrightText: 2026 Froststrap
//
// SPDX-License-Identifier: MPL-2.0

using Avalonia.Data.Converters;
using System.Text.RegularExpressions;

namespace Froststrap.UI.Converters
{
    internal class EnumNameConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not Enum enumVal)
                return value?.ToString() ?? "Unknown";

            var stringVal = enumVal.ToString();
            var type = enumVal.GetType();
            var memberInfo = type.GetMember(stringVal).FirstOrDefault();

            if (memberInfo?.GetCustomAttributes(typeof(EnumNameAttribute), false).FirstOrDefault() is EnumNameAttribute attribute)
            {
                if (!string.IsNullOrEmpty(attribute.StaticName))
                    return attribute.StaticName;

                if (!string.IsNullOrEmpty(attribute.FromTranslation))
                    return Strings.ResourceManager.GetString(attribute.FromTranslation, CultureInfo.CurrentCulture) ?? attribute.FromTranslation;
            }

            // Resource keys intentionally use Enums.<Type>.<Value>. Never expose a
            // missing resource key (for example "Enums.Theme.Cyan") to the UI.
            string resourceKey = $"Enums.{type.Name}.{stringVal}";
            string? translated = Strings.ResourceManager.GetString(resourceKey, CultureInfo.CurrentCulture);
            if (!string.IsNullOrWhiteSpace(translated))
                return translated;

            return Regex.Replace(stringVal, "(?<!^)([A-Z])", " $1");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
