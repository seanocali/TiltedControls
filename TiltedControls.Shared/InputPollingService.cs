using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Gaming.Input;
using Windows.System;
using Windows.UI.Core;

#if NETFX_CORE
using Windows.UI.Xaml;
#else
using Microsoft.UI.Xaml;
#endif

namespace TiltedControls
{
    internal static class InputPollingService
    {
        static bool _hasStarted;
        public static bool HasInitialized;
        static bool _isRawPolling;
        static DispatcherTimer _initializationCooldownTimer;
        static HashSet<IGameController> _gamepads;
        static Dictionary<RawGameController, RawGameControllerModel> _rawControllers;
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
                _rawControllers = new Dictionary<RawGameController, RawGameControllerModel>();
                for (int i = 0; i < RawGameController.RawGameControllers.Count(); i++)
                {
                    var raw = RawGameController.RawGameControllers[i];
                    if (i == 0) { RawGameController_RawGameControllerAdded(null, RawGameController.RawGameControllers.First()); }
                    else
                    {
                        _rawControllers.Add(raw, new RawGameControllerModel(raw));
                    }
                }
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

        static async void StartRawPolling()
        {
            if (_isRawPolling)
            {
                await StopRawPollingAndWait();
            }
            _isRawPolling = true;
            while (_isRawPolling)
            {
                foreach (var pair in _controllers)
                {
                    if (pair.Value == null)
                    {
                        var rawOnlyController = pair.Key;
                        var model = _rawControllers[pair.Key];
                        rawOnlyController.GetCurrentReading(model.Buttons, model.Switches, model.Axes);
                        await ParseRawReading(model);
                    }
                }
                await Task.Delay(50);
            }
        }

        static async Task ParseRawReading(RawGameControllerModel model)
        {
            for (int i = 0; i < model.Buttons.Length; i++)
            {
                if (model.Buttons[i])
                {
                    var label = model.Controller.GetButtonLabel(i);
                    Debug.WriteLine(label);
                }
            }


            if (model.Buttons.Any(x => x) 
                || model.Switches.Any(y => y != GameControllerSwitchPosition.Center))
            {
                await HandleRawInput(model.Controller);
            }
        }

        static void StopRawPolling()
        {
            _isRawPolling = false;
        }

        static async Task StopRawPollingAndWait()
        {
            _isRawPolling = false;
            await Task.Delay(50);
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
                _controllers[raw.Key] = null;
            }
            foreach (var gamepad in _gamepads)
            {
                var raw = RawGameController.FromGameController(gamepad);
                _controllers[raw] = gamepad;
            }

            if (_controllers.Values.Any(x => x == null))
            {
                StartRawPolling();
            }
            else
            {
                StopRawPolling();
            }
        }

        private static void RawGameController_RawGameControllerRemoved(object sender, RawGameController e)
        {
            if (_rawControllers.ContainsKey(e))
            {
                _rawControllers.Remove(e);
                UpdateControllerLists();
            }
        }

        private static async void RawGameController_RawGameControllerAdded(object sender, RawGameController e)
        {
            if (e.AxisCount > 0 || e.ButtonCount > 0 || e.SwitchCount > 0)
            {
                if (!_rawControllers.ContainsKey(e))
                {
                    _rawControllers.Add(e, new RawGameControllerModel(e));
                    UpdateControllerLists();
                    if (HasInitialized || VendorId == null)
                    {
                        await HandleRawInput(e);
                    }
                }
            }
        }

        internal static ushort? VendorId { get; set; }
        internal static ushort? ProductId { get; set; }
        internal static bool IsKeyboard
        {
            get => VendorId == null;
        }

        private static async void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            args.Handled = false;
            await SetLastUsedHardwareDevice(args.VirtualKey);
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

        private static async Task SetLastUsedHardwareDevice(VirtualKey key)
        {
            int keyCode = (int)key;
            if (keyCode >= 195)
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
                            await HandleRawInput(gamepad);
                            break;
                        }
                    }
                    else if (controller is ArcadeStick arcadeStick)
                    {
                        ArcadeStickReading reading = arcadeStick.GetCurrentReading();
                        if (reading.Buttons != ArcadeStickButtons.None)
                        {
                            await HandleRawInput(arcadeStick);
                            break;
                        }
                    }
                    else if (controller is RacingWheel racingWheel)
                    {
                        RacingWheelReading reading = racingWheel.GetCurrentReading();
                        if (reading.Buttons != RacingWheelButtons.None 
                            || reading.Wheel > 0.1)
                        {
                            await HandleRawInput(racingWheel);
                            break;
                        }
                    }
                }
            }
            else if (keyCode < 195)
            {
                if (VendorId != null || ProductId != null)
                {
                    VendorId = null;
                    ProductId = null;
                    OnLastInputTypeChanged();
                }
            }
        }

        static async Task HandleRawInput(IGameController controller)
        {
            var raw = RawGameController.FromGameController(controller);
            await HandleRawInput(raw);
        }

        static async Task HandleRawInput(RawGameController raw)
        {
            if (VendorId != raw.HardwareProductId || ProductId != raw.HardwareProductId)
            {
                VendorId = raw.HardwareVendorId;
                ProductId = raw.HardwareProductId;
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    OnLastInputTypeChanged();
                });
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
