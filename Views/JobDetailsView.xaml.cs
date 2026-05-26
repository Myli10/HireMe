using FirebaseWorkout.ViewModels;

namespace FirebaseWorkout.Views;

public partial class JobDetailsView : ContentPage
{
    public JobDetailsView(JobDetailsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as JobDetailsViewModel)?.OnAppearing();
    }
}
