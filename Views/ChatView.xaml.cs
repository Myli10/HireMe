using FirebaseWorkout.ViewModels;

namespace FirebaseWorkout.Views;

public partial class ChatView : ContentPage
{
    public ChatView(ChatViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        vm.ScrollToBottom = ScrollToLatestMessage;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as ChatViewModel)?.OnAppearing();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        (BindingContext as ChatViewModel)?.OnDisappearing();
    }

    private void ScrollToLatestMessage()
    {
        var vm = BindingContext as ChatViewModel;
        if (vm?.Messages.Count > 0)
            MessagesView.ScrollTo(vm.Messages[^1], animate: false);
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
