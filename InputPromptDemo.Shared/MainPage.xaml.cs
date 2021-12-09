using System;
using TiltedControls;

#if NETFX_CORE
using Microsoft.Toolkit.Uwp.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace InputPromptDemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

        }

        private void XboxOneButton_Click(object sender, RoutedEventArgs e)
        {
            SimulateGamepadInput(1118, 721);
        }

        private void Xbox360Button_Click(object sender, RoutedEventArgs e)
        {
            SimulateGamepadInput(1118, 702);
        }

        private void PS4Button_Click(object sender, RoutedEventArgs e)
        {
            SimulateGamepadInput(1356, 1476);
        }

        private void PS5Button_Click(object sender, RoutedEventArgs e)
        {
            SimulateGamepadInput(1356, 3302);
        }
        private void SwitchPro_Click(object sender, RoutedEventArgs e)
        {
            SimulateGamepadInput(1406, 3302);
        }

        private void KeyboardSim_Click(object sender, RoutedEventArgs e)
        {
            SimulateGamepadInput(null, null);
        }

        void SimulateGamepadInput(ushort? vendorId, ushort? productId)
        {
            var descendants = this.FindDescendants();
            foreach (var descendant in descendants)
            {
                if (descendant is InputPrompt inputPrompt)
                {
                    inputPrompt.SimulateInput(vendorId, productId);
                }
            }
        }

        private void InputPrompt_ImageLoaded(object sender, EventArgs e)
        {
            if (sender is InputPrompt ip)
            {
                PageViewModel.VendorId = ip.VendorId;
                PageViewModel.ProductId = ip.ProductId;
            }
        }
    }
}
