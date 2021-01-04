using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InputPromptDemo
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private bool _monochrome;

        public bool Monochrome
        {
            get { return _monochrome; }
            set 
            { 
                _monochrome = value;
                OnPropertyChanged(nameof(Monochrome));
            }
        }

        private string _vendorId;

        public string VendorId
        {
            get { return _vendorId; }
            set 
            { 
                if (value != _vendorId)
                {
                    _vendorId = value;
                    OnPropertyChanged(nameof(VendorId));
                }
            }
        }


        private string _productId;

        public string ProductId
        {
            get { return _productId; }
            set
            {
                if (value != _productId)
                {
                    _productId = value;
                    OnPropertyChanged(nameof(ProductId));
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, e);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }
    }
}
