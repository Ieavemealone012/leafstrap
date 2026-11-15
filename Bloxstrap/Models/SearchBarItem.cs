namespace Bloxstrap.Models
{
    public class SearchBarItem
    {
        public string DisplayName { get; set; } = string.Empty;
        public Type PageType { get; set; } = null!;

        public override string ToString() => DisplayName;
    }
}
