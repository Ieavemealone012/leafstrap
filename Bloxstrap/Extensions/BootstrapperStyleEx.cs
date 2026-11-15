namespace Bloxstrap.Extensions
{
    static class BootstrapperStyleEx
    {
        public static IBootstrapperDialog GetNew(this BootstrapperStyle bootstrapperStyle) => Frontend.GetBootstrapperDialog(bootstrapperStyle);

        public static IReadOnlyCollection<BootstrapperStyle> Selections => new BootstrapperStyle[]
        {
            BootstrapperStyle.ClassicFluentDialog,
            BootstrapperStyle.Roblox2016Dialog,
            BootstrapperStyle.FluentAeroDialog,
            BootstrapperStyle.FluentDialog,
            BootstrapperStyle.ByfronDialog,
            BootstrapperStyle.TwentyFiveDialog,
            BootstrapperStyle.ProgressDialog,
            BootstrapperStyle.LegacyDialog2011,
            BootstrapperStyle.LegacyDialog2008,
            BootstrapperStyle.VistaDialog,
            BootstrapperStyle.CustomDialog
        };
    }
}
