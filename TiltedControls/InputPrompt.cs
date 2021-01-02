using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using static TiltedControls.Common;
using static TiltedControls.InputPollingService;

namespace TiltedControls
{
    public class InputPrompt : ContentPresenter
    {
        Image _image;
        SvgImageSource _source;
        Assembly _assembly;

        public InputPrompt()
        {
            _image = new Image();
            _source = new SvgImageSource();
            _image.Source = _source;
            this.Content = _image;
            _assembly = this.GetType().GetTypeInfo().Assembly;
            this.Loaded += InputPrompt_Loaded;
            this.Unloaded += InputPrompt_Unloaded;
        }

        private void InputPrompt_Unloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            InputPollingService.LastInputTypeChanged -= InputPollingService_LastInputTypeChanged;
        }

        private void InputPrompt_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ushort? vendorId = null;
            ushort? productId = null;
            try
            {
                if (InitialVendorId > 0 && GetVendorName((ushort)InitialVendorId) != null) { vendorId = (ushort)InitialVendorId; }
                if (InitialProductId > 0 && GetProductName((ushort)InitialProductId) != null) { productId = (ushort)InitialProductId; }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            InputPollingService.LastInputTypeChanged += InputPollingService_LastInputTypeChanged;
            InputPollingService.Start(vendorId, productId);
            Refresh();
        }

        private void InputPollingService_LastInputTypeChanged(object sender, EventArgs e)
        {
            Refresh();
        }

        async void Refresh()
        {
            bool success = false;
            string rootFolderName = null;
            string themeName = null;
            string productName = null;
            string keyName = null;
            var key = GamepadKey;
            rootFolderName = InputPollingService.VendorId != null ? GetVendorName((ushort)InputPollingService.VendorId) : null;
            if (MonochromePreferred && HasMonochromeFont(rootFolderName))
            {
                string resourceName = $"TiltedControls.Resources.Fonts.ps4.ttf";
                var font = new FontFamily(resourceName + "#ps4");
                var vb = new Viewbox();
                var tb = new TextBlock();
                tb.Foreground = this.Foreground;
                tb.FontFamily = font;
                tb.Text = "\uE803";
                vb.Child = tb;
                this.Content = vb;
            }
            else
            {
                //if (this.Content != _image) { this.Content = _image; }
                if (!InputPollingService.IsKeyboard)
                {
                    productName = GetProductName((ushort)InputPollingService.VendorId, InputPollingService.ProductId);
                    if (productName != null) { productName += '-'; }
                    keyName = GetGamepadKeyName(key, (ushort)InputPollingService.VendorId, InputPollingService.ProductId).Replace("Gamepad", "");
                }
                if (rootFolderName == null)
                {
                    rootFolderName = "Keyboard";
                    themeName = Theme == ApplicationTheme.Dark ? "Dark." : "Light.";
                    keyName = MappedKeyboardKey != null ? MappedKeyboardKey : GetDefaultKeyboardKeyName(key);
                }
                string resourceName = $"TiltedControls.Resources.InputPromptImages.{rootFolderName}.{themeName}{productName}{keyName}.svg";

                try
                {
                    using (var resourceStream = _assembly.GetManifestResourceStream(resourceName))
                    {
                        if (resourceStream != null && resourceStream.Length > 0)
                        {
                            using (var memStream = new MemoryStream())
                            {
                                await resourceStream.CopyToAsync(memStream);
                                memStream.Position = 0;
                                using (var raStream = memStream.AsRandomAccessStream())
                                {
                                    SvgImageSourceLoadStatus status = await _source.SetSourceAsync(raStream);
                                    success = status == 0;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            if (success) { OnImageLoaded(); }
            else { OnImageLoadFailed(); }
        }

        private bool HasMonochromeFont(string vendorName)
        {
            if (vendorName != null)
            {
                vendorName = vendorName.ToLower();
                switch (vendorName)
                {
                    case "microsoft":
                    case "sony":
                        return true;
                }
            }
            return false;
        }

        public event EventHandler ImageLoaded = delegate { };
        void OnImageLoaded()
        {
            EventHandler handler = ImageLoaded;
            handler(null, new EventArgs());
        }

        public event EventHandler ImageLoadFailed = delegate { };
        void OnImageLoadFailed()
        {
            _source.UriSource = null;
            EventHandler handler = ImageLoadFailed;
            handler(null, new EventArgs());
        }

        private static string GetVendorName(ushort vendorId)
        {
            switch (vendorId)
            {
                case 1118:
                    return "Microsoft";
                case 1356:
                    return "Sony";
                case 1406:
                    return "Nintendo";
                case 10462:
                    return "Valve";
            }
            return null;
        }

        private static string GetProductName(ushort vendorId, ushort? productId = null)
        {
            switch (vendorId)
            {
                case 1118:
                    return "Xbox One";
                case 1356:
                    return "PS4";
                case 1406:
                    return "Switch";
                case 10462:
                    return "Steam";
            }
            return null;
        }

        private static string GetGamepadKeyName(GamepadInputTypes key, ushort vendorId, ushort? productId = null)
        {
            switch (vendorId)
            {
                case 1356:
                    if (productId != null)
                    {
                        switch ((ushort)productId)
                        {

                        }
                    }
                    return GetPS4KeyName(key);
                case 1406:
                    if (productId != null)
                    {
                        switch ((ushort)productId)
                        {

                        }
                    }
                    return GetSwitchKeyName(key);
            }
            return key.ToString();
        }

        public static string GetPS3KeyName(GamepadInputTypes key)
        {
            switch (key)
            {
                case GamepadInputTypes.A:
                    return "Cross";
                case GamepadInputTypes.B:
                    return "Circle";
                case GamepadInputTypes.X:
                    return "Square";
                case GamepadInputTypes.Y:
                    return "Triangle";
                case GamepadInputTypes.LeftShoulder:
                    return "L1";
                case GamepadInputTypes.RightShoulder:
                    return "R1";
                case GamepadInputTypes.LeftTrigger:
                    return "L2";
                case GamepadInputTypes.RightTrigger:
                    return "R2";
                case GamepadInputTypes.LeftThumbstickButton:
                    return "L3";
                case GamepadInputTypes.RightThumbstickButton:
                    return "R3";
                case GamepadInputTypes.View:
                    return "Select";
                case GamepadInputTypes.Menu:
                    return "Start";
                default:
                    return key.ToString();
            }
        }

        public static string GetPS4KeyName(GamepadInputTypes key)
        {
            switch (key)
            {
                case GamepadInputTypes.View:
                    return "Share";
                case GamepadInputTypes.Menu:
                    return "Options";
                default:
                    return GetPS3KeyName(key);
            }
        }

        public static string GetPS5KeyName(GamepadInputTypes key)
        {
            switch (key)
            {
                case GamepadInputTypes.View:
                    return "Create";
                default:
                    return GetPS4KeyName(key);
            }
        }

        public static string GetSwitchKeyName(GamepadInputTypes key)
        {
            switch (key)
            {
                case GamepadInputTypes.LeftShoulder:
                    return "L";
                case GamepadInputTypes.RightShoulder:
                    return "R";
                default:
                    return key.ToString();
            }
        }

        public static string GetDefaultKeyboardKeyName(GamepadInputTypes key)
        {
            switch (key)
            {
                case GamepadInputTypes.A:
                    return "Enter";
                case GamepadInputTypes.B:
                    return "Back";
                case GamepadInputTypes.X:
                    return "Space";
                case GamepadInputTypes.Y:
                    return "~";
                case GamepadInputTypes.View:
                    return "Home";
                case GamepadInputTypes.Menu:
                    return "Escape";
                case GamepadInputTypes.LeftShoulder:
                    return "ChevronLeft";
                case GamepadInputTypes.RightShoulder:
                    return "ChevronRight";
                case GamepadInputTypes.LeftRightShoulder:
                    return "ChevronLeftRight";
                case GamepadInputTypes.LeftTrigger:
                    return "BracketLeft";
                case GamepadInputTypes.RightTrigger:
                    return "BracketRight";
                case GamepadInputTypes.LeftRightTrigger:
                    return "BracketLeftRight";
                case GamepadInputTypes.DPad:
                    return "UpDownLeftRight";
                case GamepadInputTypes.DPadUp:
                    return "Up";
                case GamepadInputTypes.DPadDown:
                    return "Down";
                case GamepadInputTypes.DPadLeft:
                    return "Left";
                case GamepadInputTypes.DPadRight:
                    return "Right";
                case GamepadInputTypes.DPadUpLeft:
                    return "UpLeft";
                case GamepadInputTypes.DPadDownRight:
                    return "DownRight";
                case GamepadInputTypes.DPadDownLeft:
                    return "DownLeft";
                case GamepadInputTypes.DPadUpRight:
                    return "UpRight";
                case GamepadInputTypes.LeftThumbstick:
                    return "WSAD";
                case GamepadInputTypes.LeftThumbstickClockwise:
                    return "R";
                case GamepadInputTypes.LeftThumbstickCounterclockwise:
                    return "Q";
                case GamepadInputTypes.LeftThumbstickUp:
                    return "W";
                case GamepadInputTypes.LeftThumbstickDown:
                    return "S";
                case GamepadInputTypes.LeftThumbstickLeft:
                    return "A";
                case GamepadInputTypes.LeftThumbstickRight:
                    return "D";
                case GamepadInputTypes.LeftThumbstickUpLeft:
                    return "WS";
                case GamepadInputTypes.LeftThumbstickDownRight:
                    return "SD";
                case GamepadInputTypes.LeftThumbstickDownLeft:
                    return "SA";
                case GamepadInputTypes.LeftThumbstickUpRight:
                    return "WD";
                case GamepadInputTypes.LeftThumbstickButton:
                    return "ChevronLeft";
                case GamepadInputTypes.RightThumbstick:
                    return "8246";
                case GamepadInputTypes.RightThumbstickClockwise:
                    return "9";
                case GamepadInputTypes.RightThumbstickCounterclockwise:
                    return "7";
                case GamepadInputTypes.RightThumbstickUp:
                    return "8";
                case GamepadInputTypes.RightThumbstickDown:
                    return "2";
                case GamepadInputTypes.RightThumbstickLeft:
                    return "4";
                case GamepadInputTypes.RightThumbstickRight:
                    return "6";
                case GamepadInputTypes.RightThumbstickUpLeft:
                    return "84";
                case GamepadInputTypes.RightThumbstickDownRight:
                    return "26";
                case GamepadInputTypes.RightThumbstickDownLeft:
                    return "24";
                case GamepadInputTypes.RightThumbstickUpRight:
                    return "86";
                case GamepadInputTypes.RightThumbstickButton:
                    return "ChevronRight";
                case GamepadInputTypes.HomeButton:
                    return "End";
            }
            return null;
        }

        private static void OnMapPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as InputPrompt;
            if (control.IsLoaded)
            {
                control.Refresh();
            }
        }

        #region DEPENDENCY PROPERTIES

        public GamepadInputTypes GamepadKey
        {
            get
            {
                return (GamepadInputTypes)base.GetValue(GamepadKeyProperty);
            }
            set
            {
                base.SetValue(GamepadKeyProperty, value);
            }
        }
        public static readonly DependencyProperty GamepadKeyProperty = DependencyProperty.Register(nameof(GamepadKey), typeof(GamepadInputTypes), typeof(InputPrompt),
            new PropertyMetadata(null, OnMapPropertyChanged));

        public bool MonochromePreferred
        {
            get
            {
                return (bool)base.GetValue(MonochromePreferredProperty);
            }
            set
            {
                base.SetValue(MonochromePreferredProperty, value);
            }
        }
        public static readonly DependencyProperty MonochromePreferredProperty = DependencyProperty.Register(nameof(MonochromePreferred), typeof(bool), typeof(InputPrompt),
            new PropertyMetadata(false, OnMapPropertyChanged));

        public int InitialVendorId
        {
            get
            {
                return (int)base.GetValue(InitialVendorIdProperty);
            }
            set
            {
                base.SetValue(InitialVendorIdProperty, value);
            }
        }
        public static readonly DependencyProperty InitialVendorIdProperty = DependencyProperty.Register(nameof(InitialVendorId), typeof(int), typeof(InputPrompt),
            null);

        public int InitialProductId
        {
            get
            {
                return (int)base.GetValue(InitialProductIdProperty);
            }
            set
            {
                base.SetValue(InitialProductIdProperty, value);
            }
        }
        public static readonly DependencyProperty InitialProductIdProperty = DependencyProperty.Register(nameof(InitialProductId), typeof(int), typeof(InputPrompt),
            null);

        public ApplicationTheme Theme
        {
            get
            {
                return (ApplicationTheme)base.GetValue(ThemeProperty);
            }
            set
            {
                base.SetValue(ThemeProperty, value);
            }
        }
        public static readonly DependencyProperty ThemeProperty = DependencyProperty.Register(nameof(Theme), typeof(ApplicationTheme), typeof(InputPrompt),
            new PropertyMetadata(null, OnMapPropertyChanged));

        public string MappedKeyboardKey
        {
            get
            {
                return (string)base.GetValue(MappedKeyboardKeyProperty);
            }
            set
            {
                base.SetValue(MappedKeyboardKeyProperty, value);
            }
        }
        public static readonly DependencyProperty MappedKeyboardKeyProperty = DependencyProperty.Register(nameof(MappedKeyboardKey), typeof(string), typeof(InputPrompt),
            new PropertyMetadata(null, OnMapPropertyChanged));

        #endregion

    }

}
