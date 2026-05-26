using FirebaseWorkout.ViewModels;

namespace FirebaseWorkout.Views;

public partial class SignUpView : ContentPage
{
	public SignUpView(SignUpViewModel vm)
	{
		InitializeComponent();		
		vm.Navigation = this.Navigation;
		BindingContext = vm;
	}
}