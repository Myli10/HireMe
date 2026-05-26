using FirebaseWorkout.ViewModels;

namespace FirebaseWorkout.Views;

public partial class AddJobView : ContentPage
{
    public AddJobView(AddJobViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

}
