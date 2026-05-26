using FirebaseWorkout.Model;
using FirebaseWorkout.ViewModels;

namespace FirebaseWorkout.Views;

public partial class FavoriteJobsView : ContentPage
{
    public FavoriteJobsView(FavoriteJobsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as FavoriteJobsViewModel)?.OnAppearing();
    }

    private void OnFavoriteToggled(object sender, ToggledEventArgs e)
    {
        if (sender is Switch sw && sw.BindingContext is FavoriteWorkPlaceItem item)
        {
            (BindingContext as FavoriteJobsViewModel)?.ToggleFavoriteCommand.Execute(item);
        }
    }
}
