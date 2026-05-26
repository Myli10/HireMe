using FirebaseWorkout.ViewModels;

namespace FirebaseWorkout.Views;

public partial class AddReviewView : ContentPage
{
    public AddReviewView(AddReviewViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
