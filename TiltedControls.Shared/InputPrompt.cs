#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
#else
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
#endif
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static TiltedControls.Common;

namespace TiltedControls
{
    public sealed class InputPrompt : ContentPresenter
    {
        Image _image;
        SvgImageSource _source;
        Assembly _assembly;
        CancellationTokenSource _cts;

        static FontFamily _xboxOneMonochromeFont;
        static FontFamily _ps4MonochromeFont;

        public InputPrompt()
        {
#if !NETFX_CORE
            throw new NotImplementedException("Input Prompt only works on UWP apps at this time.");
#endif
            _image = new Image();
            _source = new SvgImageSource();
            _image.Source = _source;
            this.Content = _image;
            _assembly = this.GetType().GetTypeInfo().Assembly;
            this.Loaded += InputPrompt_Loaded;
            this.Unloaded += InputPrompt_Unloaded;
        }

        private void InputPrompt_Unloaded(object sender, RoutedEventArgs e)
        {
            InputPollingService.LastInputTypeChanged -= InputPollingService_LastInputTypeChanged;
        }

        private async void InputPrompt_Loaded(object sender, RoutedEventArgs e)
        {
            ushort? vendorId = null;
            ushort? productId = null;
            try
            {
                if (InitialVendorId > 0 && GetVendorName((ushort)InitialVendorId) != null) { vendorId = (ushort)InitialVendorId; }
                if (InitialProductId > 0 && GetProductName((ushort)InitialProductId) != null) { productId = (ushort)InitialProductId; }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            
            UIElement rootUIElement = null;
#if !NETFX_CORE
            rootUIElement = this;
            while (true)
            {
                var ascendant = rootUIElement.FindAscendant<UIElement>();
                if (ascendant != null) { rootUIElement = ascendant; }
                else { break; }
            }
#endif
            InputPollingService.LastInputTypeChanged += InputPollingService_LastInputTypeChanged;
            InputPollingService.Start(rootUIElement, vendorId, productId);
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
            var key = ReverseAxes ? GetReverseAxis(GamepadKey) : GamepadKey;
            ushort? vendorId = simulatedVendorId != null ? simulatedVendorId : InputPollingService.VendorId;
            ushort? productId = simulatedProductId != null ? simulatedProductId : InputPollingService.ProductId;

            rootFolderName = vendorId != null ? GetVendorName((ushort)vendorId) : null;
            productName = vendorId != null && productId != null ? GetProductName((ushort)vendorId, productId) : null;

            string monochromeFontName = Monochrome != MonochromeModes.None ? GetMonochromeFontName(rootFolderName, productName, Monochrome == MonochromeModes.Force) : null;
            if (monochromeFontName != null)
            {

                var vb = new Viewbox();
                var tb = new TextBlock();
                tb.Foreground = this.Foreground;
                tb.FontFamily = LoadAndGetFont(monochromeFontName);
                var hexStr = "E8" + ((int)key).ToString("X2");
                int decValue = int.Parse(hexStr, System.Globalization.NumberStyles.HexNumber);
                var c = (char)decValue;
                tb.Text = c.ToString();
                vb.Child = tb;
                this.Content = vb;
            }
            else
            {
                if (this.Content is Image image && image == _image) { this.Content = _image; }
                if (!InputPollingService.IsKeyboard)
                {
                    if (productName != null) { productName += '-'; }
                    keyName = GetGamepadKeyName(key, (ushort)vendorId, productId).Replace("Gamepad", "");
                }
                if (rootFolderName == null)
                {
                    rootFolderName = "Keyboard";
                    themeName = Monochrome != MonochromeModes.None ? "Monochrome" : "";
                    themeName += Theme == ApplicationTheme.Dark ? "Dark." : "Light.";
                    keyName = MappedKeyboardKey != null ? MappedKeyboardKey : GetDefaultKeyboardKeyName(key);
                }
                var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
                string resourceName = $"{assemblyName}.Resources.InputPromptImages.{rootFolderName}.{themeName}{productName}{keyName}.svg";

                await UpdateImageFromEmbeddedResource(resourceName, cancellationToken);
            }
        }

        private FontFamily LoadAndGetFont(string requestedFont)
        {
            switch (requestedFont)
            {
                case "xboxone":
                    if (_xboxOneMonochromeFont == null) { _xboxOneMonochromeFont = new FontFamily("/TiltedControls/Resources/Fonts/xboxone.ttf#xboxone"); }
                    return _xboxOneMonochromeFont;
                case "ps4":
                    if (_ps4MonochromeFont == null) { _ps4MonochromeFont = new FontFamily("/TiltedControls/Resources/Fonts/ps4.ttf#ps4"); }
                    return _ps4MonochromeFont;
            }
            return null;
        }

        private async Task UpdateImageFromEmbeddedResource(string resourceName, CancellationToken cancellationToken)
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

        private async Task UpdateImageFromSvgString(string svgString, CancellationToken cancellationToken)
        {
            bool success = false;
            try
            {
                using (var memStream = new MemoryStream())
                using (var writer = new StreamWriter(memStream))
                {
                    memStream.Position = 0;
                    writer.Write(memStream);
                    writer.Flush();
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
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            if (success) { OnImageLoaded(); }
            else { OnImageLoadFailed(); }
        }

        private string GetMonochromeFontName(string vendorName, string productName, bool force = false)
        {
            if (vendorName != null)
            {
                vendorName = vendorName.ToLower();
                if (force)
                {
                    switch (vendorName)
                    {
                        default:
                        case "microsoft":
                            return "xboxone";
                        case "sony":
                            return "ps4";
                    }
                }
                else
                {
                    return GetExactMonochromeGamepadName(productName);
                }
            }
            return null;
        }

        string GetExactMonochromeGamepadName(string productName)
        {
            switch (productName)
            {
                default:
                    return null;
                case "Xbox One":
                    return "xboxone";
                case "PS4":
                    return "ps4";
            }
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

        private GamepadInputTypes GetReverseAxis(GamepadInputTypes gamepadKey)
        {
            switch (gamepadKey)
            {
                default:
                    return gamepadKey;
                case GamepadInputTypes.DPadUpDown:
                    return GamepadInputTypes.DPadLeftRight;
                case GamepadInputTypes.DPadLeftRight:
                    return GamepadInputTypes.DPadUpDown;
                case GamepadInputTypes.LeftThumbstickLeftRight:
                    return GamepadInputTypes.LeftThumbstickUpDown;
                case GamepadInputTypes.LeftThumbstickUpDown:
                    return GamepadInputTypes.LeftThumbstickLeftRight;
                case GamepadInputTypes.RightThumbstickLeftRight:
                    return GamepadInputTypes.RightThumbstickUpDown;
                case GamepadInputTypes.RightThumbstickUpDown:
                    return GamepadInputTypes.RightThumbstickLeftRight;
            }
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

        public MonochromeModes Monochrome
        {
            get
            {
                return (MonochromeModes)base.GetValue(MonochromeProperty);
            }
            set
            {
                base.SetValue(MonochromeProperty, value);
            }
        }
        public static readonly DependencyProperty MonochromeProperty = DependencyProperty.Register(nameof(Monochrome), typeof(MonochromeModes), typeof(InputPrompt),
            new PropertyMetadata(MonochromeModes.None, OnMapPropertyChanged));

        public bool ReverseAxes
        {
            get
            {
                return (bool)base.GetValue(ReverseAxesProperty);
            }
            set
            {
                base.SetValue(ReverseAxesProperty, value);
            }
        }
        public static readonly DependencyProperty ReverseAxesProperty = DependencyProperty.Register(nameof(ReverseAxes), typeof(bool), typeof(InputPrompt),
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

        static string Test
        {
            get
            {
                return @"<?xml version=""1.0"" encoding=""UTF-8""?><!--This file is compatible with Silverlight--><Canvas xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" Name=""Layer_1"" Canvas.Left=""0"" Canvas.Top=""0"" Width=""100"" Height=""100"">  <Canvas.RenderTransform>    <TranslateTransform X=""0"" Y=""0""/>  </Canvas.RenderTransform>  <Canvas.Resources/>  <!--Unknown tag: metadata-->  <!--Unknown tag: sodipodi:namedview-->  <Canvas Name=""g69"">    <Canvas.RenderTransform>      <MatrixTransform Matrix=""1 0 0 1 0 0""/>    </Canvas.RenderTransform>    <Canvas Name=""g64"">      <Canvas Name=""Layer1_0_FILL"">        <Path xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" Name=""path61"" Fill=""#FF000000"" Data=""M52.6 55c-0.4-0.4-0.8-0.6-1.3-0.8s-1-0.3-1.6-0.3c-0.3 0-0.5 0-0.8 0.2c-0.3 0.1-0.5 0.1-0.7 0.3     c-0.2 0.1-0.4 0.2-0.5 0.4c-0.1 0.1-0.3 0.3-0.4 0.4v-1H45v11.6h2.5v-4.6h0c0.2 0.4 0.6 0.7 1 0.8c0.4 0.2 0.9 0.3 1.5 0.3     c0.5 0 1-0.1 1.5-0.4c0.4-0.2 0.8-0.5 1.2-1c0.3-0.4 0.5-0.8 0.7-1.3c0.1-0.5 0.2-1 0.2-1.5c0-0.6-0.1-1.2-0.3-1.7     C53.1 55.8 52.9 55.4 52.6 55 M50.7 56.5c0.3 0.4 0.5 0.8 0.5 1.5c0 0.6-0.2 1.1-0.5 1.5c-0.3 0.4-0.8 0.5-1.5 0.5     s-1.1-0.2-1.5-0.5c-0.3-0.4-0.5-0.8-0.5-1.5s0.2-1.1 0.5-1.5c0.3-0.4 0.8-0.5 1.5-0.5S50.4 56.2 50.7 56.5 M62.3 58     c0-0.7-0.1-1.3-0.2-1.8c-0.1-0.5-0.3-0.9-0.6-1.3c-0.3-0.3-0.6-0.6-1-0.8c-0.5-0.2-1.1-0.3-1.8-0.3c-0.6 0-1.3 0.1-1.9 0.3     c-0.6 0.2-1.1 0.5-1.5 0.9l1.3 1.4c0.2-0.3 0.5-0.5 0.8-0.7c0.4-0.2 0.8-0.3 1.2-0.3s0.8 0.1 1 0.4c0.3 0.2 0.5 0.6 0.5 1     c-0.3 0-0.7 0-1.1 0c-0.4 0-0.8 0-1.2 0c-0.4 0.1-0.8 0.2-1.2 0.3c-0.4 0.1-0.7 0.3-1 0.5c-0.3 0.2-0.5 0.5-0.7 0.8     c-0.1 0.3-0.2 0.7-0.2 1.2c0 0.4 0.1 0.8 0.2 1c0.2 0.3 0.4 0.6 0.6 0.8c0.3 0.2 0.6 0.4 1 0.5c0.3 0.1 0.7 0.2 1 0.2     c0.5 0 0.9-0.1 1.4-0.3c0.4-0.2 0.8-0.5 1.1-1v1h2.3V58 M58.7 58.5c0.3 0 0.5 0 0.7 0H60V59c0 0.3 0 0.5-0.1 0.7     c-0.1 0.2-0.3 0.3-0.5 0.4c-0.2 0.1-0.4 0.2-0.6 0.3c-0.2 0.1-0.4 0.1-0.7 0.1c-0.2 0-0.5-0.1-0.8-0.3c-0.3-0.1-0.4-0.4-0.4-0.7     c0-0.2 0.1-0.4 0.3-0.6c0.2-0.2 0.4-0.3 0.7-0.3C58.2 58.6 58.5 58.5 58.7 58.5 M69.4 54.1c-0.5-0.2-1-0.3-1.5-0.3     c-0.6 0-1.1 0.1-1.7 0.3c-0.5 0.2-1 0.4-1.4 0.8c-0.4 0.4-0.7 0.8-0.9 1.3c-0.2 0.5-0.4 1.1-0.4 1.7c0 0.6 0.1 1.2 0.4 1.7     c0.2 0.5 0.6 1 0.9 1.3c0.4 0.3 0.9 0.6 1.4 0.8s1.1 0.3 1.7 0.3c0.5 0 1-0.1 1.5-0.3c0.5-0.1 0.9-0.4 1.3-0.8l-1.6-1.7     c-0.1 0.2-0.3 0.3-0.5 0.4C68.5 60 68.2 60 67.9 60c-0.6 0-1.1-0.2-1.4-0.5c-0.4-0.4-0.6-0.8-0.6-1.5s0.2-1.1 0.6-1.5     c0.3-0.4 0.8-0.5 1.4-0.5c0.3 0 0.5 0 0.8 0.2c0.2 0.1 0.4 0.2 0.5 0.4l1.6-1.7C70.4 54.5 69.9 54.2 69.4 54.1 M78.5 55     c-0.3-0.4-0.7-0.6-1.2-0.8c-0.5-0.2-1-0.3-1.5-0.3c-0.6 0-1.2 0.1-1.7 0.3c-0.5 0.2-1 0.4-1.4 0.8c-0.4 0.4-0.7 0.8-0.9 1.3     c-0.2 0.5-0.3 1.1-0.3 1.7c0 0.6 0.1 1.2 0.3 1.7s0.5 1 0.9 1.3c0.4 0.3 0.9 0.6 1.4 0.8c0.5 0.2 1.1 0.3 1.7 0.3     c0.6 0 1.3-0.1 1.9-0.4c0.6-0.3 1.1-0.7 1.5-1.2l-1.8-1.3c-0.2 0.3-0.4 0.5-0.7 0.7c-0.3 0.2-0.7 0.3-1 0.3     c-0.5 0-0.9-0.1-1.2-0.4c-0.3-0.3-0.5-0.7-0.7-1.2h5.7V58c0-0.6-0.1-1.2-0.3-1.7C79.1 55.8 78.8 55.3 78.5 55 M74.3 56.1     c0.2-0.1 0.3-0.3 0.5-0.4c0.2-0.1 0.5-0.1 0.7-0.1c0.4 0 0.8 0.2 1.1 0.5c0.3 0.3 0.4 0.6 0.4 1h-3.3c0-0.2 0.1-0.4 0.2-0.5     C74.1 56.4 74.2 56.2 74.3 56.1 M39.8 50.2c-0.6 0-1.2 0.1-1.7 0.3c-0.5 0.1-1 0.4-1.4 0.7c-0.4 0.3-0.7 0.7-1 1.2     c-0.2 0.5-0.3 1-0.3 1.6c0 0.7 0.1 1.2 0.4 1.6c0.3 0.4 0.6 0.7 1 0.9c0.4 0.3 0.9 0.5 1.3 0.6c0.4 0.2 0.9 0.3 1.3 0.4     c0.4 0.1 0.7 0.3 1 0.6c0.2 0.2 0.4 0.4 0.4 0.8c0 0.2 0 0.4-0.1 0.5c-0.1 0.1-0.2 0.3-0.4 0.3c-0.2 0.1-0.4 0.2-0.6 0.3     s-0.4 0.1-0.6 0.1c-0.4 0-0.8-0.1-1.2-0.3c-0.4-0.2-0.7-0.4-1-0.8l-1.8 2c0.6 0.5 1.2 0.9 1.8 1.1c0.7 0.2 1.4 0.3 2 0.3     c0.6 0 1.2-0.1 1.8-0.2c0.5-0.1 1-0.4 1.4-0.7c0.4-0.3 0.7-0.7 1-1.2c0.2-0.5 0.3-1 0.3-1.6c0-0.7-0.1-1.3-0.3-1.7     c-0.3-0.4-0.6-0.7-1-0.9c-0.4-0.3-0.8-0.5-1.3-0.6c-0.4-0.2-0.8-0.3-1.3-0.4c-0.4-0.1-0.7-0.3-1-0.5C38.1 54.3 38 54 38 53.7     c0-0.2 0-0.4 0.1-0.6c0.1-0.1 0.3-0.2 0.5-0.3c0.2-0.1 0.3-0.2 0.5-0.2c0.2-0.1 0.4-0.1 0.6-0.1c0.3 0 0.6 0.1 1 0.3     c0.4 0.1 0.6 0.3 0.8 0.5l1.8-1.9c-0.5-0.4-1-0.7-1.7-0.9C41 50.2 40.4 50.2 39.8 50.2 M38 43.2c-0.3 0-0.7 0-1.1 0     c-0.4 0-0.8 0-1.2 0c-0.4 0.1-0.8 0.2-1.1 0.3c-0.4 0.1-0.7 0.3-1 0.5c-0.3 0.2-0.5 0.5-0.7 0.8c-0.1 0.3-0.2 0.7-0.2 1.2     c0 0.4 0.1 0.8 0.2 1c0.1 0.3 0.3 0.6 0.6 0.8c0.2 0.2 0.5 0.4 0.9 0.5c0.3 0.1 0.7 0.2 1.1 0.2c0.5 0 0.9-0.1 1.4-0.3     c0.4-0.2 0.8-0.6 1.1-1v1h2.3v-4c0-0.6-0.1-1.2-0.2-1.7s-0.3-0.9-0.6-1.3c-0.3-0.3-0.6-0.6-1-0.8c-0.5-0.2-1.1-0.3-1.8-0.3     c-0.7 0-1.3 0.1-1.9 0.3c-0.6 0.2-1.1 0.5-1.6 1l1.3 1.3c0.3-0.3 0.6-0.5 0.9-0.7c0.4-0.2 0.8-0.3 1.2-0.3c0.4 0 0.7 0.1 1 0.4     C37.8 42.4 38 42.8 38 43.2 M35 45.9c0-0.3 0.1-0.5 0.3-0.6c0.2-0.2 0.4-0.3 0.7-0.3c0.2-0.1 0.5-0.2 0.8-0.2c0.3 0 0.5 0 0.7 0     H38v0.5c0 0.3 0 0.5-0.1 0.7c-0.1 0.2-0.3 0.3-0.4 0.5c-0.2 0.1-0.4 0.2-0.6 0.3c-0.2 0.1-0.4 0.1-0.7 0.1s-0.5-0.1-0.8-0.2     C35.1 46.4 35 46.2 35 45.9 M27.1 36.8h-4.3v11.5h4.8c0.5 0 1-0.1 1.5-0.2c0.5-0.1 1-0.3 1.4-0.5c0.4-0.3 0.8-0.6 1.1-1     c0.2-0.4 0.3-0.9 0.3-1.5c0-0.4 0-0.7-0.1-1c-0.1-0.3-0.3-0.6-0.5-0.8c-0.2-0.3-0.5-0.5-0.8-0.6c-0.3-0.2-0.7-0.3-1-0.3v0     c0.6-0.2 1-0.5 1.4-0.9c0.4-0.4 0.5-1 0.5-1.6c0-0.6-0.1-1.1-0.3-1.5c-0.3-0.4-0.6-0.7-1-0.9c-0.4-0.2-0.9-0.3-1.5-0.4     C28.1 36.8 27.6 36.8 27.1 36.8 M28.2 39.2c0.2 0.1 0.3 0.2 0.4 0.3c0.1 0.2 0.2 0.4 0.2 0.6c0 0.3-0.1 0.5-0.2 0.6     c-0.1 0.2-0.2 0.3-0.4 0.3c-0.2 0.1-0.3 0.2-0.5 0.3c-0.2 0.1-0.4 0.1-0.6 0.1h-1.9v-2.5H27c0.2 0 0.4 0 0.6 0.1     C27.9 38.9 28 39 28.2 39.2 M25.3 46v-2.6h2c0.2 0 0.4 0 0.6 0c0.3 0 0.5 0.1 0.6 0.2c0.2 0.1 0.4 0.2 0.6 0.3     c0.2 0.2 0.3 0.4 0.3 0.7c0 0.2-0.1 0.4-0.2 0.6c-0.1 0.2-0.2 0.4-0.4 0.5S28.4 46 28.2 46c-0.2 0-0.4 0-0.6 0H25.3 M54.8 40.3     L52 43.5V36h-2.5v12.3H52v-4h0l2.7 4h3l-3.2-4.3l3.2-3.6H54.8 M46.5 42.5c0.2 0.1 0.4 0.2 0.5 0.4l1.6-1.6     c-0.4-0.4-0.8-0.7-1.3-0.9c-0.6-0.1-1.1-0.2-1.6-0.2c-0.6 0-1.1 0.1-1.6 0.3c-0.6 0.2-1.1 0.4-1.5 0.8s-0.7 0.8-0.9 1.3     c-0.3 0.5-0.4 1.1-0.4 1.7c0 0.6 0.1 1.2 0.4 1.7c0.2 0.5 0.5 0.9 0.9 1.3c0.4 0.4 0.9 0.6 1.5 0.8c0.5 0.2 1.1 0.4 1.6 0.4     c0.5 0 1-0.1 1.6-0.3c0.5-0.2 0.9-0.5 1.3-0.9l-1.6-1.6c-0.1 0.2-0.3 0.3-0.5 0.4c-0.2 0.1-0.4 0.2-0.7 0.2     c-0.6 0-1.1-0.2-1.4-0.5c-0.4-0.4-0.5-0.9-0.5-1.5c0-0.6 0.2-1.1 0.5-1.5c0.3-0.4 0.8-0.5 1.4-0.5C46.1 42.3 46.4 42.3 46.5 42.5     z""/>      </Canvas>    </Canvas>    <Canvas Name=""g67"">      <Path xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" Name=""Layer1_0_2_STROKES"" StrokeThickness=""5"" Stroke=""#FF000000"" StrokeLineJoin=""Round"" StrokeStartLineCap=""Round"" StrokeEndLineCap=""Round"" Data=""    M87.9 22.1c3.3 0 5 1.7 5 5c-0.7 15.6-0.7 31.2 0 46.7c0 3.3-1.7 5-5 5H13.1c-3.3 0-5-1.7-5-5c0.7-15.5 0.7-31.1 0-46.7    c0-3.3 1.7-5 5-5H87.9z""/>    </Canvas>  </Canvas></Canvas>";
            }
        }

    }

}
