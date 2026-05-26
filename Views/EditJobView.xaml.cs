using FirebaseWorkout.ViewModels;

namespace FirebaseWorkout.Views;

public partial class EditJobView : ContentPage
{
    public EditJobView(EditJobViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
