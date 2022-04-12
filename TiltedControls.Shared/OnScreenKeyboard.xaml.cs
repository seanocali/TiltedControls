using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace TiltedControls
{
    public sealed partial class OnScreenKeyboard : UserControl
    {
        public OnScreenKeyboard()
        {
            var b = new Button();
            this.InitializeComponent();
        }

        private void B_Clicked(object sender, RoutedEventArgs e)
        {
            HandleInput(sender);
        }

        void HandleInput(object sender)
        {
            if (sender is Button button)
            {
                if (button.Content is String s)
                {
                    VirtualKey key = default(VirtualKey);
                    switch (s.ToLower())
                    {
                        case "space":
                            key = VirtualKey.Space;
                            break;
                        case "back":
                            key = VirtualKey.Back;
                            break;
                        case "enter":
                            key = VirtualKey.Enter;
                            break;
                        default:
                            var c = s.FirstOrDefault();
                            key = (VirtualKey)c;
                            break;
                    }
                    OnKeyClick(new KeyClickEventArgs { Key = key });
                }
            }
           
        }
        public event EventHandler<KeyClickEventArgs> KeyClick;
        public void OnKeyClick(KeyClickEventArgs e)
        {
            KeyClick?.Invoke(this, e);
        }
    }

    public class KeyClickEventArgs : EventArgs
    {
        public VirtualKey Key { get; set; }
    }
}
