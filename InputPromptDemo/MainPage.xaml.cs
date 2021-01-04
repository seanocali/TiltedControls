using Microsoft.Toolkit.Uwp.UI.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TiltedControls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

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

        }

        void SimulateGamepadInput(ushort vendorId, ushort? productId)
        {
            var controls = this.FindDescendants<InputPrompt>();
            foreach (var control in controls)
            {
                control.SimulateInput(vendorId, productId);
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
