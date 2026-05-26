using FirebaseWorkout.Model;
using FirebaseWorkout.ViewModels;

namespace FirebaseWorkout.Views;

public partial class MyChatsView : ContentPage
{
    public MyChatsView(MyChatsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as MyChatsViewModel)?.OnAppearing();
    }

    private async void OnOpenChatClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is UserChatRoom room)
        {
            var param = new Dictionary<string, object>
            {
                { "workPlaceId", room.WorkPlaceId },
                { "workPlaceName", room.WorkPlaceName },
                { "otherUserId", string.Empty },
                { "otherUserName", room.ManagerName }
            };
            await Shell.Current.GoToAsync("ChatView", param);
        }
    }
}
