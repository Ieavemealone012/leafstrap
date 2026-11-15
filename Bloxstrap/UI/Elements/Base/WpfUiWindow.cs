﻿using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Mvvm.Contracts;
using Wpf.Ui.Mvvm.Services;

namespace Bloxstrap.UI.Elements.Base
{
    public abstract class WpfUiWindow : UiWindow
    {
        // I could add animated backgrounds, its easy but its hella gay i need to add a seperate image control :/
        private readonly IThemeService _themeService = new ThemeService();

        public WpfUiWindow()
        {
            ApplyTheme();
        }

        public void ApplyTheme()
        {
            const int customThemeIndex = 2;

            var finalTheme = App.Settings.Prop.Theme.GetFinal();

            bool classicDark = finalTheme == Enums.Theme.Roblox2016 && Extensions.ThemeEx.IsSystemDark();
            bool useLightBase = finalTheme == Enums.Theme.Light || (finalTheme == Enums.Theme.Roblox2016 && !classicDark);
            _themeService.SetTheme(useLightBase ? ThemeType.Light : ThemeType.Dark);
            _themeService.SetSystemAccent();

            ApplyThemeFont(finalTheme);

            // Custom themes set this key directly. Remove it before applying a
            // standard theme so it cannot mask the new dictionary's background.
            Application.Current.Resources.Remove("ApplicationBackground");

            // Keep an opaque WPF UI base behind transparent theme overlays.
            // A null window background exposes the DWM/system accent surface,
            // which can turn the entire Dark or Light theme into the accent color.
            SetResourceReference(BackgroundProperty, "ApplicationBackgroundBrush");

            if (finalTheme == Enums.Theme.Custom)
            {
                if (App.Settings.Prop.BackgroundType == BackgroundMode.Gradient)
                {
                    ApplyGradientBackground();
                }
                else if (App.Settings.Prop.BackgroundType == BackgroundMode.Image)
                {
                    ApplyImageBackground();
                }

                ApplyCustomThemeResources();
            }
            else
            {
                ApplyStandardTheme(finalTheme, customThemeIndex);
            }

#if QA_BUILD
    this.BorderBrush = System.Windows.Media.Brushes.Red;
    this.BorderThickness = new Thickness(4);
#endif
        }

        private void ApplyThemeFont(Enums.Theme finalTheme)
        {
            string? customFontPath = App.Settings.Prop.CustomFontPath;
            if (!string.IsNullOrWhiteSpace(customFontPath) && File.Exists(customFontPath))
            {
                var customFont = FontManager.LoadFontFromFile(customFontPath);
                if (customFont != null)
                {
                    FontFamily = customFont;
                    return;
                }
            }

            FontFamily = finalTheme == Enums.Theme.Roblox2016
                ? (System.Windows.Media.FontFamily)Application.Current.Resources["ComicNeueAngular"]
                : SystemFonts.MessageFontFamily;
        }

        private void ApplyGradientBackground()
        {
            double angle = App.Settings.Prop.GradientAngle;
            double angleRad = angle * Math.PI / 180.0;

            double startX = 0.5 + 0.5 * Math.Cos(angleRad + Math.PI);
            double startY = 0.5 + 0.5 * Math.Sin(angleRad + Math.PI);
            double endX = 0.5 + 0.5 * Math.Cos(angleRad);
            double endY = 0.5 + 0.5 * Math.Sin(angleRad);

            var customBrush = new LinearGradientBrush
            {
                StartPoint = new Point(startX, startY),
                EndPoint = new Point(endX, endY)
            };

            foreach (var stop in App.Settings.Prop.CustomGradientStops.OrderBy(s => s.Offset))
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(stop.Color);
                    customBrush.GradientStops.Add(new GradientStop(color, stop.Offset));
                }
                catch { }
            }

            Application.Current.Resources["ApplicationBackground"] = customBrush;
        }

        private void ApplyImageBackground()
        {
            if (string.IsNullOrEmpty(App.Settings.Prop.BackgroundImagePath) || !File.Exists(App.Settings.Prop.BackgroundImagePath))
            {
                return;
            }

            try
            {
                var imageSource = new BitmapImage();
                imageSource.BeginInit();
                imageSource.CacheOption = BitmapCacheOption.OnLoad;
                imageSource.UriSource = new Uri(App.Settings.Prop.BackgroundImagePath);
                imageSource.EndInit();
                imageSource.Freeze();

                var imageBrush = new ImageBrush
                {
                    ImageSource = imageSource,
                    Stretch = App.Settings.Prop.BackgroundStretch switch
                    {
                        BackgroundStretch.None => Stretch.None,
                        BackgroundStretch.Fill => Stretch.Fill,
                        BackgroundStretch.Uniform => Stretch.Uniform,
                        BackgroundStretch.UniformToFill => Stretch.UniformToFill,
                        _ => Stretch.UniformToFill
                    },
                    Opacity = App.Settings.Prop.BackgroundOpacity
                };

                Application.Current.Resources["ApplicationBackground"] = imageBrush;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("WpfUiWindow", $"Exception when changing to image: {ex.Message}");
            }
        }

        private void ApplyCustomThemeResources()
        {
            Application.Current.Resources["NewTextEditorBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#59000000"));
            Application.Current.Resources["NewTextEditorForeground"] = new SolidColorBrush(Colors.White);
            Application.Current.Resources["NewTextEditorLink"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A9CEA"));
            Application.Current.Resources["PrimaryBackgroundColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#19000000"));
            Application.Current.Resources["NormalDarkAndLightBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0FFFFFFF"));
            Application.Current.Resources["ControlFillColorDefault"] = (Color)ColorConverter.ConvertFromString("#19000000");
        }

        private void ApplyStandardTheme(Enums.Theme finalTheme, int customThemeIndex)
        {
            string themeName = finalTheme == Enums.Theme.Roblox2016
                ? (Extensions.ThemeEx.IsSystemDark() ? "Roblox2016Dark" : "Roblox2016Light")
                : Enum.GetName(finalTheme)!;

            var dict = new ResourceDictionary { Source = new Uri($"pack://application:,,,/UI/Style/{themeName}.xaml") };
            Application.Current.Resources.MergedDictionaries[customThemeIndex] = dict;

            Application.Current.Resources.Remove("NewTextEditorBackground");
            Application.Current.Resources.Remove("NewTextEditorForeground");
            Application.Current.Resources.Remove("NewTextEditorLink");
            Application.Current.Resources.Remove("PrimaryBackgroundColor");
            Application.Current.Resources.Remove("NormalDarkAndLightBackground");
            Application.Current.Resources.Remove("ControlFillColorDefault");
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Hardware Accel
            if (App.Settings.Prop.WPFSoftwareRender || App.LaunchSettings.NoGPUFlag.Active)
            {
                if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
                    hwndSource.CompositionTarget.RenderMode = RenderMode.SoftwareOnly;
            }

            // Custom Font
            string? fontPath = App.Settings.Prop.CustomFontPath;
            if (!string.IsNullOrWhiteSpace(fontPath) && File.Exists(fontPath))
            {
                var font = FontManager.LoadFontFromFile(fontPath);
                if (font != null)
                {
                    this.FontFamily = font;
                }
            }
        }
    }
}
