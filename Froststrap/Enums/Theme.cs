// SPDX-FileCopyrightText: 2026 Froststrap
//
// SPDX-License-Identifier: MPL-2.0

namespace Froststrap.Enums
{
    internal enum Theme
    {
        [EnumName(FromTranslation = "Common.SystemDefault")]
        Default,
        [EnumName(StaticName = "Dark")]
        Dark,
        [EnumName(StaticName = "Light")]
        Light,
        [EnumName(FromTranslation = "Common.Custom")]
        Custom,
        [EnumName(StaticName = "Leafstrap")]
        Leafstrap,
        [EnumName(StaticName = "Roblox Classic (2016)")]
        Roblox2016,
        [EnumName(StaticName = "Haunted Leaf (Halloween)")]
        Halloween,
        [EnumName(StaticName = "Cyan")]
        Cyan,
        [EnumName(StaticName = "Purple")]
        Purple,
        [EnumName(StaticName = "Blue")]
        Blue,
        [EnumName(StaticName = "Orange")]
        Orange,
        [EnumName(StaticName = "Pink")]
        Pink
    }
}
