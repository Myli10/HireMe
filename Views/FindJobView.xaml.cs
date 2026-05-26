using FirebaseWorkout.Model;
using FirebaseWorkout.ViewModels;

namespace FirebaseWorkout.Views;

public partial class FindJobView : ContentPage
{
    public FindJobView(FindJobViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as FindJobViewModel)?.OnAppearing();
    }

    private void OnMoreButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is WorkPlace workPlace)
        {
            (BindingContext as FindJobViewModel)?.GoToDetailsCommand.Execute(workPlace);
        }
    }
}
