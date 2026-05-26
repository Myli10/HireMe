using FirebaseWorkout.ViewModels;

namespace FirebaseWorkout.Views;

public partial class AdminView : ContentPage
{
	public AdminView(AdminViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}