using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FirebaseWorkout.Model;
using FirebaseWorkout.Service;
using FirebaseWorkout.Service.DBService;
using System.Collections.ObjectModel;

namespace FirebaseWorkout.ViewModels
{
    // מסך הצ'אטים של המועמד — רשימת השיחות שלו עם מנהלים
    public partial class MyChatsViewModel : ObservableObject
    {
        private readonly IChatRepository _chatRepository;
        private readonly IWorkPlaceRepository _workPlaceRepository;
        private readonly IAppLogger _appLogger;

        [ObservableProperty] private bool _isBusy;

        public ObservableCollection<UserChatRoom> ChatRooms { get; } = new();

        public MyChatsViewModel(IChatRepository chatRepository, IWorkPlaceRepository workPlaceRepository, IAppLogger appLogger)
        {
            _chatRepository = chatRepository;
            _workPlaceRepository = workPlaceRepository;
            _appLogger = appLogger;
        }

        internal async void OnAppearing() => await LoadAsync();

        private async Task LoadAsync()
        {
            var userId = (App.Current as App)?.CurrentUser?.Id;
            if (string.IsNullOrEmpty(userId)) return;

            IsBusy = true;
            try
            {
                var rooms = await _chatRepository.GetUserChatsAsync(userId);

                // Firebase שומר workPlaceId בלבד — טוענים את כל המשרות כדי לצרף שמות
                var allWorkPlaces = await _workPlaceRepository.GetAllWorkPlacesAsync();

                // ToDictionary: מילון { "id" → WorkPlace } לחיפוש מהיר במקום לולאה
                var wpMap = allWorkPlaces.ToDictionary(w => w.Id, w => w);

                ChatRooms.Clear();
                foreach (var room in rooms)
                {
                    // TryGetValue: אם ה-id קיים במילון — קח את השם, אחרת השתמש ב-id כגיבוי
                    if (wpMap.TryGetValue(room.WorkPlaceId, out var wp))
                        room.WorkPlaceName = wp.Name ?? room.WorkPlaceId;
                    ChatRooms.Add(room);
                }
            }
            catch (Exception ex) { _appLogger.LogDebug($"LoadAsync: {ex.Message}"); }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task GoBack() => await Shell.Current.GoToAsync("..");
    }
}
