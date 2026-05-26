using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FirebaseWorkout.Model;
using FirebaseWorkout.Service;
using FirebaseWorkout.Service.DBService;

namespace FirebaseWorkout.ViewModels
{
    // ===================================================
    // AddReviewViewModel — לוגיקת מסך כתיבת ביקורת
    // ===================================================
    // מאפשר למשתמש לכתוב ביקורת על מקום עבודה.
    // מקבל שני פרמטרים מ-JobDetailsView דרך [QueryProperty]:
    //   - workPlaceId: מזהה המשרה
    //   - workPlaceName: שם המשרה (להצגה בכותרת)
    //
    // לאחר שליחה: מעדכן את דירוג המשרה ב-Firebase
    // ===================================================
    [QueryProperty(nameof(WorkPlaceId), "workPlaceId")]
    [QueryProperty(nameof(WorkPlaceName), "workPlaceName")]
    public partial class AddReviewViewModel : ObservableObject
    {
        private readonly IWorkPlaceRepository _workPlaceRepository;
        private readonly IAlertService _alertService;
        private readonly IAppLogger _appLogger;

        // מזהה המשרה — מגיע מ-JobDetailsView
        [ObservableProperty] private string _workPlaceId = string.Empty;

        // שם המשרה — מוצג בכותרת המסך
        [ObservableProperty] private string _workPlaceName = string.Empty;

        // תוכן הביקורת הטקסטואלי
        [ObservableProperty] private string _reviewText = string.Empty;

        // דירוג שנבחר (1-5) — ברירת מחדל: 3
        [ObservableProperty] private int _selectedRating = 3;

        // האם בתהליך שמירה
        [ObservableProperty] private bool _isBusy;

        // אפשרויות הדירוג לבחירה ב-Picker (1 עד 5 כוכבים)
        public List<int> RatingOptions { get; } = new() { 1, 2, 3, 4, 5 };

        // Computed Property: מציג את הדירוג הנוכחי כטקסט (לדוגמה: "4 / 5")
        public string CurrentStars => $"{SelectedRating} / 5";

        // נקרא אוטומטית כשהדירוג משתנה — מעדכן את תצוגת CurrentStars
        partial void OnSelectedRatingChanged(int value) => OnPropertyChanged(nameof(CurrentStars));

        public AddReviewViewModel(IWorkPlaceRepository workPlaceRepository, IAlertService alertService, IAppLogger appLogger)
        {
            _workPlaceRepository = workPlaceRepository;
            _alertService = alertService;
            _appLogger = appLogger;
        }

        // שליחת הביקורת ל-Firebase.
        // Firebase מעדכן גם את דירוג הממוצע (WorkerRating) ומספר הביקורות (ReviewCount).
        [RelayCommand]
        private async Task SubmitReview()
        {
            // בדיקה שהמשתמש כתב ביקורת
            if (string.IsNullOrWhiteSpace(ReviewText))
            {
                await _alertService.ShowAlertAsync("Missing Info", "Please write your review.", "OK");
                return;
            }

            IsBusy = true;
            try
            {
                var currentUser = (App.Current as App)!.CurrentUser!;

                // יצירת אובייקט הביקורת לשמירה
                var review = new Review
                {
                    WorkPlaceId = WorkPlaceId,
                    UserId = currentUser.Id,
                    UserName = $"{currentUser.FirstName} {currentUser.LastName}".Trim(),
                    Text = ReviewText.Trim(),
                    Rating = SelectedRating,                         // 1-5 כוכבים
                    Date = DateTime.Now.ToString("dd/MM/yyyy")       // תאריך הביקורת
                };

                // שמירה ב-Firebase — הפונקציה גם מעדכנת את הדירוג הממוצע
                await _workPlaceRepository.AddReviewAsync(review);
                await _alertService.ShowAlertAsync("Thank You!", "Your review was submitted.", "OK");
                await Shell.Current.GoToAsync(".."); // חזרה לפרטי המשרה
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"SubmitReview failed: {ex.Message}");
                await _alertService.ShowAlertAsync("Error", "Could not submit review. Please try again.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ביטול — חזרה לפרטי המשרה ללא שמירה
        [RelayCommand]
        private async Task Cancel()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
