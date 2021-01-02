using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Gaming.Input;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace TiltedControls
{
    internal static class InputPollingService
    {
        static bool _hasStarted;
        public static bool HasInitialized;
        static DispatcherTimer _initializationCooldownTimer;
        static HashSet<IGameController> _gamepads;
        static HashSet<RawGameController> _rawControllers;
        static Dictionary<RawGameController, IGameController> _controllers = new Dictionary<RawGameController, IGameController>();

        static InputPollingService()
        {
            _initializationCooldownTimer = new DispatcherTimer();
            _initializationCooldownTimer.Tick += _initializationCooldownTimer_Tick;
            _initializationCooldownTimer.Interval = TimeSpan.FromSeconds(1);
        }

        internal static void Start(ushort? initialVendorId = null, ushort? initialProductId = null)
        {
            if (!_hasStarted)
            {
                _hasStarted = true;
                if (initialVendorId != null)
                {
                    VendorId = (ushort)initialVendorId;
                    if (initialProductId != null)
                    {
                        ProductId = (ushort)initialProductId;
                    }
                }

                IEnumerable<IGameController> controllers = Gamepad.Gamepads.AsEnumerable();
                controllers.Concat(ArcadeStick.ArcadeSticks.AsEnumerable());
                controllers.Concat(RacingWheel.RacingWheels.AsEnumerable());

                _gamepads = controllers.ToHashSet();
                _rawControllers = RawGameController.RawGameControllers.ToHashSet();

                Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
                Gamepad.GamepadAdded += Controller_Added;
                Gamepad.GamepadRemoved += Controller_Removed;
                ArcadeStick.ArcadeStickAdded += Controller_Added;
                ArcadeStick.ArcadeStickRemoved += Controller_Removed;
                RacingWheel.RacingWheelAdded += Controller_Added;
                RacingWheel.RacingWheelRemoved += Controller_Removed;
                RawGameController.RawGameControllerAdded += RawGameController_RawGameControllerAdded;
                RawGameController.RawGameControllerRemoved += RawGameController_RawGameControllerRemoved;
                UpdateControllerLists();
                _initializationCooldownTimer.Start();
            }
        }

        private static void _initializationCooldownTimer_Tick(object sender, object e)
        {
            _initializationCooldownTimer.Stop();
            HasInitialized = true;
        }

        static void UpdateControllerLists()
        {
            _controllers = new Dictionary<RawGameController, IGameController>();
            foreach (var raw in _rawControllers)
            {
                _controllers[raw] = null;
            }
            foreach (var gamepad in _gamepads)
            {
                var raw = RawGameController.FromGameController(gamepad);
                _controllers[raw] = gamepad;
            }
        }

        private static void RawGameController_RawGameControllerRemoved(object sender, RawGameController e)
        {
            _rawControllers.Remove(e);
            UpdateControllerLists();
        }

        private static async void RawGameController_RawGameControllerAdded(object sender, RawGameController e)
        {
            _rawControllers.Add(e);
            UpdateControllerLists();
            if (HasInitialized || VendorId == null)
            {
                VendorId = e.HardwareVendorId;
                ProductId = e.HardwareProductId;
#if NETFX_CORE
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
#else
                await CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, () =>
#endif
                {
                    OnLastInputTypeChanged();
                });
            }
        }

        internal static void Stop()
        {
            Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown;
            Gamepad.GamepadAdded -= Controller_Added;
            Gamepad.GamepadRemoved -= Controller_Removed;
            ArcadeStick.ArcadeStickAdded -= Controller_Added;
            ArcadeStick.ArcadeStickRemoved -= Controller_Removed;
            RacingWheel.RacingWheelAdded -= Controller_Added;
            RacingWheel.RacingWheelRemoved -= Controller_Removed;
            _controllers = null;
            HasInitialized = false;
            _hasStarted = false;
        }

        internal static ushort? VendorId { get; private set; }
        internal static ushort? ProductId { get; private set; }
        internal static bool IsKeyboard
        {
            get => VendorId == null;
        }

        private static void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            SetLastUsedHardwareDevice(args.VirtualKey);
            args.Handled = false;
        }

        private static void Controller_Added(object sender, IGameController e)
        {
            _gamepads.Add(e);
            UpdateControllerLists();
        }

        private static void Controller_Removed(object sender, IGameController e)
        {
            _gamepads.Remove(e);
            UpdateControllerLists();
        }

        private static void SetLastUsedHardwareDevice(VirtualKey key)
        {
            int keyCode = (int)key;
            if (keyCode >= 195 && (IsKeyboard))
            {
                foreach (var controller in _controllers.Values)
                {
                    if (controller is Gamepad gamepad)
                    {
                        GamepadReading reading = gamepad.GetCurrentReading();
                        if (reading.Buttons != GamepadButtons.None
                            || reading.LeftThumbstickX > 0.1 || reading.LeftThumbstickX < -0.1
                            || reading.LeftThumbstickY > 0.1 || reading.LeftThumbstickY < -0.1
                            || reading.LeftTrigger > 0.1 || reading.RightTrigger > 0.1
                            || reading.RightThumbstickX > 0.1 || reading.RightThumbstickX < -0.1
                            || reading.RightThumbstickY > 0.1 || reading.RightThumbstickY < -0.1)
                        {
                            var raw = RawGameController.FromGameController(gamepad);
                            VendorId = raw.HardwareVendorId;
                            ProductId = raw.HardwareProductId;
                            OnLastInputTypeChanged();
                            break;
                        }
                    }
                    else if (controller is ArcadeStick arcadeStick)
                    {
                        ArcadeStickReading reading = arcadeStick.GetCurrentReading();
                        if (reading.Buttons != ArcadeStickButtons.None)
                        {
                            var raw = RawGameController.FromGameController(arcadeStick);
                            VendorId = raw.HardwareVendorId;
                            ProductId = raw.HardwareProductId;
                            OnLastInputTypeChanged();
                            break;
                        }
                    }
                    else if (controller is RacingWheel racingWheel)
                    {
                        RacingWheelReading reading = racingWheel.GetCurrentReading();
                        if (reading.Buttons != RacingWheelButtons.None 
                            || reading.Wheel > 0.1)
                        {
                            var raw = RawGameController.FromGameController(racingWheel);
                            VendorId = raw.HardwareVendorId;
                            ProductId = raw.HardwareProductId;
                            OnLastInputTypeChanged();
                            break;
                        }
                    }
                }
            }
            else if (keyCode < 195 && !IsKeyboard)
            {
                VendorId = null;
                ProductId = null;
                OnLastInputTypeChanged();
            }
        }

        public static event EventHandler LastInputTypeChanged = delegate { };
        static void OnLastInputTypeChanged()
        {
            EventHandler handler = LastInputTypeChanged;
            handler(null, new EventArgs());
        }
    }
}
