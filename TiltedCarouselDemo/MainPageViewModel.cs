using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using static Tilted.Common;

namespace TiltedCarouselDemo
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        public MainPageViewModel()
        {
            CreateTestItems();
        }

        public IList<ItemModel> Items { get; set; } = new List<ItemModel>();

        int _selectedIndex;
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                if (value != _selectedIndex)
                {
                    _selectedIndex = value;
                    OnPropertyChanged(nameof(SelectedIndexText));
                    OnPropertyChanged(nameof(SelectedItemText));
                    OnPropertyChanged(nameof(SelectedItemBackground));
                }
            }
        }

        private object SelectedItem
        {
            get
            {
                return Items != null ? Items[SelectedIndex] : null;
            }
        }

        public string SelectedIndexText
        {
            get
            {
                return SelectedIndex.ToString();
            }
        }

        public string SelectedItemText
        {
            get
            {
                return SelectedItem is ItemModel model ? model.Text : null;
            }
        }

        public Brush SelectedItemBackground
        {
            get
            {
                return SelectedItem is ItemModel model ? model.BackgroundColor : null;
            }
        }

        CarouselTypes _carouselType = CarouselTypes.Wheel;
        public CarouselTypes CarouselType
        {
            get { return _carouselType; }
            set
            {
                if (value != _carouselType)
                {
                    _carouselType = value;
                    OnPropertyChanged(nameof(CarouselType));
                    OnPropertyChanged(nameof(IsWheel));
                    OnPropertyChanged(nameof(IsNotWheel));
                    OnPropertyChanged(nameof(OffsetX));
                    OnPropertyChanged(nameof(OffsetY));
                }
            }
        }

        public IEnumerable<string> CarouselTypeNames
        {
            get
            {
                return System.Enum.GetNames(typeof(CarouselTypes));
            }
        }

        public bool IsWheel
        {
            get { return CarouselType == CarouselTypes.Wheel; }
        }

        public bool IsNotWheel
        {
            get { return CarouselType != CarouselTypes.Wheel; }
        }

        public string SelectedCarouselTypeName
        {
            get
            {
                return CarouselType.ToString();
            }
            set
            {
                if (Enum.TryParse(typeof(CarouselTypes), value, out var result))
                {
                    CarouselType = (CarouselTypes)result;
                }
            }
        }

        WheelAlignments _wheelAlignment = WheelAlignments.Right;
        public WheelAlignments WheelAlignment
        {
            get { return _wheelAlignment; }
            set
            {
                if (value != _wheelAlignment)
                {
                    _wheelAlignment = value;
                    OnPropertyChanged(nameof(WheelAlignment));
                    OnPropertyChanged(nameof(OffsetX));
                    OnPropertyChanged(nameof(OffsetY));
                }
            }
        }

        public IEnumerable<string> WheelAlignmentNames
        {
            get
            {
                return System.Enum.GetNames(typeof(WheelAlignments));
            }
        }

        public string SelectedWheelAlignmentName
        {
            get
            {
                return WheelAlignment.ToString();
            }
            set
            {
                if (Enum.TryParse(typeof(WheelAlignments), value, out var result))
                {
                    WheelAlignment = (WheelAlignments)result;
                }
            }
        }

        public int OffsetX
        {
            get
            {
                switch (CarouselType)
                {
                    case CarouselTypes.Column:
                        return 350;
                    case CarouselTypes.Wheel:
                        switch (WheelAlignment)
                        {
                            case WheelAlignments.Left:
                                return -750;
                            case WheelAlignments.Right:
                                return 750;
                        }
                        break;
                }
                return 0;
            }
        }

        public int OffsetY
        {
            get
            {
                switch (CarouselType)
                {
                    case CarouselTypes.Row:
                        return 350;
                    case CarouselTypes.Wheel:
                        switch (WheelAlignment)
                        {
                            case WheelAlignments.Top:
                                return -750;
                            case WheelAlignments.Bottom:
                                return 750;
                        }
                        break;
                }
                return 0;
            }
        }

        int _navigationSpeed = 500;
        public int NavigationSpeed
        {
            get { return _navigationSpeed; }
            set
            {
                if (value != _navigationSpeed)
                {
                    _navigationSpeed = value;
                    OnPropertyChanged(nameof(NavigationSpeed));
                }
            }
        }

        float _selectedItemScale = 1.5f;
        public float SelectedItemScale
        {
            get { return _selectedItemScale; }
            set
            {
                if (value != _selectedItemScale)
                {
                    _selectedItemScale = value;
                    OnPropertyChanged(nameof(SelectedItemScale));
                }
            }
        }

        int _itemGap;
        public int ItemGap
        {
            get { return _itemGap; }
            set
            {
                if (value != _itemGap)
                {
                    _itemGap = value;
                    OnPropertyChanged(nameof(ItemGap));
                }
            }
        }

        private int _density = 48;
        public int Density
        {
            get { return _density; }
            set
            {
                if (_density != value)
                {
                    _density = value;
                    OnPropertyChanged(nameof(Density));
                }
            }
        }

        private float _fliptychDegrees;
        public float FliptychDegrees
        {
            get { return _fliptychDegrees; }
            set
            {
                if (_fliptychDegrees != value)
                {
                    _fliptychDegrees = value;
                    OnPropertyChanged(nameof(FliptychDegrees));
                }
            }
        }

        private int _warpIntensity;
        public int WarpIntensity
        {
            get { return _warpIntensity; }
            set
            {
                if (_warpIntensity != value)
                {
                    _warpIntensity = value;
                    OnPropertyChanged(nameof(WarpIntensity));
                }
            }
        }

        private double _warpCurve;
        public double WarpCurve
        {
            get { return _warpCurve; }
            set
            {
                if (_warpCurve != value)
                {
                    _warpCurve = value;
                    OnPropertyChanged(nameof(WarpCurve));
                }
            }
        }

        private string _indexChooserInput;
        public string IndexChooserInput
        {
            get { return _indexChooserInput; }
            set 
            { 
                if (Items != null && Int32.TryParse(value, out var input))
                {
                    if (input >= Items.Count())
                    {
                        input = Items.Count() - 1;
                    }
                    SelectedIndex = input;
                    OnPropertyChanged(nameof(SelectedIndex));
                    _indexChooserInput = value;

                }
            }
        }


        private void CreateTestItems()
        {
            foreach (var prop in typeof(Colors).GetProperties())
            {
                if (prop.GetValue(null) is Color color)
                {
                    Items.Add(new ItemModel { BackgroundColor = new SolidColorBrush(color), Text = prop.Name });
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
