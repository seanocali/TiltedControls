using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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
    public sealed class InputPrompt : ContentPresenter
    {
        Image _image;
        SvgImageSource _source;
        Assembly _assembly;
        CancellationTokenSource _cts;

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

        private async void InputPrompt_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
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
            await Refresh();
        }

        private async void InputPollingService_LastInputTypeChanged(object sender, EventArgs e)
        {
            await Refresh();
        }

        public async void SimulateInput(ushort vendorId, ushort? productId)
        {
            InputPollingService.ProductId = productId;
            InputPollingService.VendorId = vendorId;
            await Refresh(vendorId, productId);
        }

        async Task Refresh(ushort? simulatedVendorId = null, ushort? simulatedProductId = null)
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
            }
            _cts = new CancellationTokenSource();
            var cancellationToken = _cts.Token;
            string rootFolderName = null;
            string themeName = null;
            string productName = null;
            string keyName = null;
            var key = GamepadKey;
            if (simulatedVendorId != null)
            {
                rootFolderName = GetVendorName((ushort)simulatedVendorId);
            }
            else
            {
                rootFolderName = InputPollingService.VendorId != null ? GetVendorName((ushort)InputPollingService.VendorId) : null;
            }

            if (MonochromePreferred && GetMonochromeFontName(rootFolderName) != null)
            {
                var fontName = GetMonochromeFontName(rootFolderName);
                var font = new FontFamily($"TiltedControls/Resources/Fonts/{fontName}.ttf#{fontName}");
                var vb = new Viewbox();
                var tb = new TextBlock();
                tb.Foreground = this.Foreground;
                tb.FontFamily = font;

                var hexStr = "E8" + ((int)key).ToString("X2");
                int decValue = int.Parse(hexStr, System.Globalization.NumberStyles.HexNumber);
                var c = (char)decValue;
                tb.Text = c.ToString();
                vb.Child = tb;
                this.Content = vb;
            }
            else
            {
                if (this.Content != _image) { this.Content = _image; }
                if (simulatedVendorId != null)
                {
                    productName = GetProductName((ushort)simulatedVendorId, simulatedProductId);
                    if (productName != null) { productName += '-'; }
                    keyName = GetGamepadKeyName(key, (ushort)simulatedVendorId, simulatedProductId).Replace("Gamepad", "");
                }
                else
                {
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
                }

                string resourceName = $"TiltedControls.Resources.InputPromptImages.{rootFolderName}.{themeName}{productName}{keyName}.svg";

                await UpdateImage(resourceName, cancellationToken);
            }
        }

        private async Task UpdateImage(string resourceName, CancellationToken cancellationToken)
        {
            bool success = false;
            try
            {
                using (var resourceStream = _assembly.GetManifestResourceStream(resourceName))
                {
                    if (resourceStream != null && resourceStream.Length > 0)
                    {
                        using (var memStream = new MemoryStream())
                        {
                            await Task.WhenAny(resourceStream.CopyToAsync(memStream), cancellationToken.AsTask());
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                memStream.Position = 0;
                                using (var raStream = memStream.AsRandomAccessStream())
                                {
                                    var setSourceTask = _source.SetSourceAsync(raStream).AsTask();
                                    await Task.WhenAny(setSourceTask, cancellationToken.AsTask());
                                    if (!cancellationToken.IsCancellationRequested)
                                    {
                                        SvgImageSourceLoadStatus status = await setSourceTask;
                                        success = status == 0;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            if (success) { OnImageLoaded(); }
            else { OnImageLoadFailed(); }
        }

        private string GetMonochromeFontName(string vendorName)
        {
            if (vendorName != null)
            {
                vendorName = vendorName.ToLower();
                switch (vendorName)
                {
                    case "microsoft":
                        return "xboxone";
                    case "sony":
                        return "ps4";
                }
            }
            return null;
        }

        public event EventHandler ImageLoaded = delegate { };
        void OnImageLoaded()
        {
            EventHandler handler = ImageLoaded;
            handler(this, new EventArgs());
        }

        public event EventHandler ImageLoadFailed = delegate { };
        void OnImageLoadFailed()
        {
            _source.UriSource = null;
            EventHandler handler = ImageLoadFailed;
            handler(this, new EventArgs());
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

        /// <summary>
        /// https://devicehunt.com/
        /// </summary>
        private static string GetProductName(ushort vendorId, ushort? productId = null)
        {
            switch (vendorId)
            {
                case 1118:
                    if (productId != null)
                    {
                        switch ((ushort)productId)
                        {
                            case 721:
                            case 746:
                            case 733:
                                return "Xbox One";
                            case 702:
                                return "Xbox 360";
                        }
                    }
                    return "Xbox One";
                case 1356:
                    if (productId != null)
                    {
                        switch ((ushort)productId)
                        {
                            case 6604:
                            case 2976:
                            case 1476:
                                return "PS4";
                        }
                    }
                    return "PS4";
                case 1406:
                    if (productId != null)
                    {
                        switch ((ushort)productId)
                        {
                            case 774:
                                return "Wii";
                        }
                    }
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
                case GamepadInputTypes.LeftRightShoulder:
                    return "L1R1";
                case GamepadInputTypes.LeftRightTrigger:
                    return "L2R2";
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
                case GamepadInputTypes.LeftRightShoulder:
                    return "LR";
                case GamepadInputTypes.LeftTrigger:
                    return "ZL";
                case GamepadInputTypes.RightTrigger:
                    return "ZR";
                case GamepadInputTypes.LeftRightTrigger:
                    return "ZLZR";
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
                case GamepadInputTypes.DPadUpDown:
                    return "UpDown";
                case GamepadInputTypes.DPadLeftRight:
                    return "LeftRight";
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
                case GamepadInputTypes.LeftThumbstickUpDown:
                    return "WS";
                case GamepadInputTypes.LeftThumbstickLeftRight:
                    return "AD";
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
                case GamepadInputTypes.RightThumbstickUpDown:
                    return "82";
                case GamepadInputTypes.RightThumbstickLeftRight:
                    return "46";
                case GamepadInputTypes.RightThumbstickButton:
                    return "ChevronRight";
                case GamepadInputTypes.HomeButton:
                    return "End";
            }
            return null;
        }

        private async static void OnMapPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as InputPrompt;
            if (control.IsLoaded)
            {
                await control.Refresh();
            }
        }

        public string VendorId
        {
            get => InputPollingService.VendorId != null ? ((ushort)InputPollingService.VendorId).ToString("X4") : null;
        }

        public string ProductId
        {
            get => InputPollingService.ProductId != null ? ((ushort)InputPollingService.ProductId).ToString("X4") : null;
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
            new PropertyMetadata(GamepadInputTypes.None, OnMapPropertyChanged));

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
