namespace CarouselDemo.MAUI
{
    public partial class MainPage : ContentPage
    {
        MainPageViewModel PageViewModel { get; set; } = new MainPageViewModel();
        public MainPage()
        {
            this.BindingContext = PageViewModel;
            CreateTestItemsColors();
            InitializeComponent();
        }

        private void CreateTestItemsColors()
        {
            foreach (var prop in typeof(Colors).GetFields())
            {
                if (prop.GetValue(null) is Color color)
                {
                    PageViewModel.Items.Add(new ItemModel { BackgroundColor = new SolidColorBrush(color), Text = prop.Name });
                }
            }
        }
    }
}