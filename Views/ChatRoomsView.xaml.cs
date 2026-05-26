using FirebaseWorkout.Model;
using FirebaseWorkout.ViewModels;

namespace FirebaseWorkout.Views;

public partial class ChatRoomsView : ContentPage
{
    public ChatRoomsView(ChatRoomsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as ChatRoomsViewModel)?.OnAppearing();
    }

    private async void OnOpenChatClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is ChatParticipant participant)
        {
            var vm = BindingContext as ChatRoomsViewModel;
            if (vm == null) return;
            var param = new Dictionary<string, object>
            {
                { "workPlaceId", vm.WorkPlaceId },
                { "workPlaceName", vm.WorkPlaceName },
                { "otherUserId", participant.ApplicantId },
                { "otherUserName", participant.ApplicantName }
            };
            await Shell.Current.GoToAsync("ChatView", param);
        }
    }
}
