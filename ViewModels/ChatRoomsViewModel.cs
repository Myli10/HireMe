using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FirebaseWorkout.Model;
using FirebaseWorkout.Service;
using FirebaseWorkout.Service.DBService;
using System.Collections.ObjectModel;

namespace FirebaseWorkout.ViewModels
{
    // מסך לבעל המשרה בלבד — רשימת מועמדים שפנו אליו בצ'אט
    [QueryProperty(nameof(WorkPlaceId), "workPlaceId")]
    [QueryProperty(nameof(WorkPlaceName), "workPlaceName")]
    public partial class ChatRoomsViewModel : ObservableObject
    {
        private readonly IChatRepository _chatRepository;
        private readonly IAppLogger _appLogger;

        [ObservableProperty] private string _workPlaceId = string.Empty;
        [ObservableProperty] private string _workPlaceName = string.Empty;
        [ObservableProperty] private bool _isBusy;

        public ObservableCollection<ChatParticipant> Participants { get; } = new();

        public ChatRoomsViewModel(IChatRepository chatRepository, IAppLogger appLogger)
        {
            _chatRepository = chatRepository;
            _appLogger = appLogger;
        }

        // _ = LoadAsync() = fire and forget: מריץ Task בלי await כי partial void לא יכולה להיות async
        partial void OnWorkPlaceIdChanged(string value) => _ = LoadAsync();

        internal async void OnAppearing() => await LoadAsync();

        private async Task LoadAsync()
        {
            if (string.IsNullOrEmpty(WorkPlaceId)) return;
            IsBusy = true;
            try
            {
                var participants = await _chatRepository.GetChatParticipantsAsync(WorkPlaceId);
                Participants.Clear();
                foreach (var p in participants) Participants.Add(p);
            }
            catch (Exception ex) { _appLogger.LogDebug($"LoadAsync: {ex.Message}"); }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task GoBack() => await Shell.Current.GoToAsync("..");
    }
}
