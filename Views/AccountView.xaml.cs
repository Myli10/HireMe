using FirebaseWorkout.ViewModels;

namespace FirebaseWorkout.Views;

public partial class AccountView : ContentPage
{
	public AccountView(AccountViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}
