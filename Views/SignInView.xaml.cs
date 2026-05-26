using FirebaseWorkout.ViewModels;

namespace FirebaseWorkout.Views;

public partial class SignInView : ContentPage
{
	public SignInView(SignInViewModel vm)
	{
		InitializeComponent();
		vm.Navigation = this.Navigation;
		BindingContext = vm;
	}
}