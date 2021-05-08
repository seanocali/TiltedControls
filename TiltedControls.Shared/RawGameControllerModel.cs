using Windows.Gaming.Input;

namespace TiltedControls
{
    public class RawGameControllerModel
    {
        public RawGameControllerModel(RawGameController controller)
        {
            Axes = new double[controller.AxisCount];
            Switches = new GameControllerSwitchPosition[controller.SwitchCount];
            Buttons = new bool[controller.ButtonCount];
            Controller = controller;
        }

        public RawGameController Controller { get; set; }

        public bool[] Buttons { get; set; }

        public GameControllerSwitchPosition[] Switches { get; set; }

        public double[] Axes { get; set; }
    }
}
