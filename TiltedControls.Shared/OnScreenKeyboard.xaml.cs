using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif


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
            var args = new KeyClickEventArgs();
            if (sender is Button button)
            {
                if (button.Content is String s)
                {
                    VirtualKey key = default(VirtualKey);
                    switch (s)
                    {
                        case "Space":
                            key = VirtualKey.Space;
                            args.KeyString = " ";
                            break;
                        case "Back":
                            key = VirtualKey.Back;
                            break;
                        case "Enter":
                            key = VirtualKey.Enter;
                            break;
                        default:
                            var c = s.FirstOrDefault();
                            if (c >= 65 && c <= 90)
                            {
                                args.ShiftState = true;
                                key = (VirtualKey)(c);
                            }
                            else if (c >= 97 && c <= 122)
                            {
                                c = s.ToUpper().FirstOrDefault();
                                key = (VirtualKey)(c);
                            }
                            args.KeyString = s;
                            break;
                    }
                    args.Key = key;
                    OnKeyClick(args);
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
        public string KeyString { get; set; }
        public bool ShiftState { get; set; }
    }
}
