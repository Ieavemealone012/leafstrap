namespace Bloxstrap.Enums
{
    public enum Theme
    {
        [EnumName(FromTranslation = "Common.SystemDefault")]
        Default,
        Dark,
        Light,
        [EnumName(StaticName = "Cyan")]
        Froststrap,
        Purple,
        Blue,
        [EnumName(StaticName = "Leafstrap")]
        Green,
        [EnumName(StaticName = "Roblox Classic (2016)")]
        Roblox2016,
        Orange,
        Pink,
        [EnumName(FromTranslation = "Common.Custom")]
        Custom,
        [EnumName(StaticName = "Haunted Leaf (Halloween)")]
        Halloween
    }
}
