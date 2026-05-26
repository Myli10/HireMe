using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FirebaseWorkout.Model;
using FirebaseWorkout.Service;
using FirebaseWorkout.Service.DBService;
using System.Collections.ObjectModel;

namespace FirebaseWorkout.ViewModels
{
    // ===================================================
    // FavoriteJobsViewModel — לוגיקת מסך המועדפים
    // ===================================================
    // מציג את כל המשרות שהמשתמש הוסיף למועדפים.
    // כל פריט ברשימה הוא FavoriteWorkPlaceItem שמכיל:
    //   - WorkPlace: אובייקט המשרה
    //   - IsFavorite: האם כרגע במועדפים (Switch לשליטה)
    //
    // הטוגל (Switch) לכל פריט מאפשר הסרה ישירה מהרשימה.
    // המסך מתרענן בכל כניסה (OnAppearing).
    // ===================================================
    public partial class FavoriteJobsViewModel : ObservableObject
    {
        private readonly IWorkPlaceRepository _workPlaceRepository;
        private readonly IAlertService _alertService;
        private readonly IAppLogger _appLogger;

        // רשימת המועדפים — כל פריט עוטף WorkPlace + IsFavorite
        public ObservableCollection<FavoriteWorkPlaceItem> FavoriteJobs { get; set; } = new();

        // האם בתהליך טעינה — מציג Spinner
        [ObservableProperty]
        private bool _isBusy;

        public FavoriteJobsViewModel(IWorkPlaceRepository workPlaceRepository, IAlertService alertService, IAppLogger appLogger)
        {
            _workPlaceRepository = workPlaceRepository;
            _alertService = alertService;
            _appLogger = appLogger;
        }

        // מטפל בשינוי Switch למועדף ספציפי.
        // IsFavorite = true → הוסף למועדפים
        // IsFavorite = false → הסר מהמועדפים ומהרשימה
        [RelayCommand]
        private async Task ToggleFavorite(FavoriteWorkPlaceItem item)
        {
            if (item == null) return;

            var userId = (App.Current as App)!.CurrentUser!.Id;
            try
            {
                if (item.IsFavorite)
                {
                    // Switch הופעל — הוסף למועדפים ב-Firebase
                    await _workPlaceRepository.AddToFavoritesAsync(userId, item.WorkPlace.Id);
                }
                else
                {
                    // Switch כובה — הסר מהמועדפים ב-Firebase וגם מהרשימה המוצגת
                    await _workPlaceRepository.RemoveFromFavoritesAsync(userId, item.WorkPlace.Id);
                    FavoriteJobs.Remove(item); // הסרה מיידית מהרשימה (ללא רענון)
                }
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"ToggleFavorite failed: {ex.Message}");

                // אם נכשל — החזר את הSwitch למצבו הקודם (מניעת UI לא עקבי)
                item.IsFavorite = !item.IsFavorite;
                await _alertService.ShowAlertAsync("Error", "Could not update favorites.", "OK");
            }
        }

        // חזרה למסך הקודם (FindJobView)
        [RelayCommand]
        private async Task BackToFindJob()
        {
            await Shell.Current.GoToAsync("..");
        }

        // נקרא בכל כניסה למסך — טוען מחדש את המועדפים מ-Firebase
        internal async void OnAppearing()
        {
            await LoadFavoriteJobsAsync();
        }

        // טוען את רשימת המועדפים של המשתמש מ-Firebase.
        // IsBusy מציג Spinner בזמן הטעינה.
        private async Task LoadFavoriteJobsAsync()
        {
            IsBusy = true;
            try
            {
                var userId = (App.Current as App)!.CurrentUser!.Id;

                // שליפת כל המשרות המועדפות של המשתמש הנוכחי
                var favorites = await _workPlaceRepository.GetFavoriteJobsAsync(userId);

                FavoriteJobs.Clear();
                foreach (var job in favorites)
                {
                    // עטיפת כל WorkPlace ב-FavoriteWorkPlaceItem עם IsFavorite = true
                    FavoriteJobs.Add(new FavoriteWorkPlaceItem
                    {
                        WorkPlace = job,
                        IsFavorite = true // כולם מועדפים בהגדרה (הגיעו מהמועדפים)
                    });
                }
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"LoadFavoriteJobsAsync failed: {ex.Message}");
                await _alertService.ShowAlertAsync("Error", "Could not load favorites.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
