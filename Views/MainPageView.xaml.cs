using FirebaseWorkout.ViewModels;

namespace FirebaseWorkout.Views;

public partial class MainPageView : ContentPage
{
	public MainPageView(MainPageViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}