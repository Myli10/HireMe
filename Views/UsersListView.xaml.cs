using FirebaseWorkout.ViewModels;

namespace FirebaseWorkout.Views;

public partial class UsersListView : ContentPage
{
	public UsersListView(UsersListViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		(BindingContext as UsersListViewModel)!.OnAppearing();
	}
	
	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		(BindingContext as UsersListViewModel)!.OnDisappearing();
	}
}