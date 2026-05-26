using FirebaseWorkout.ViewModels;

namespace FirebaseWorkout.Views;

public partial class JobMapView : ContentPage
{
    private readonly JobMapViewModel _vm;

    public JobMapView(JobMapViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(JobMapViewModel.MapHtml) && !string.IsNullOrEmpty(vm.MapHtml))
            {
                MapWebView.Source = new HtmlWebViewSource { Html = vm.MapHtml };
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.OnAppearing();
    }

    private async void OnMapNavigating(object sender, WebNavigatingEventArgs e)
    {
        // לתפוס קליק על קישור "View Details" במפה
        if (e.Url.StartsWith("hireme://workplace/"))
        {
            e.Cancel = true;
            var workPlaceId = e.Url.Replace("hireme://workplace/", "");
            var workPlace = _vm.GetWorkPlaceById(workPlaceId);
            if (workPlace != null)
            {
                var param = new Dictionary<string, object> { { "selectedWorkPlace", workPlace } };
                await Shell.Current.GoToAsync("JobDetailsView", param);
            }
        }
    }
}
