using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FirebaseWorkout.Model;
using FirebaseWorkout.Service;
using FirebaseWorkout.Service.DBService;
using System.Collections.ObjectModel;

namespace FirebaseWorkout.ViewModels
{
    // ===================================================
    // JobDetailsViewModel — לוגיקת מסך פרטי משרה
    // ===================================================
    // [QueryProperty] = מקבל את אובייקט המשרה מהמסך הקודם (FindJobView).
    // מסך זה מציג פרטים מלאים של משרה ופעולות:
    //   למועמד:    הוספה למועדפים, כתיבת ביקורת, צ'אט עם מנהל, דיווח
    //   לבעל משרה: סימון מאויש, ניהול צ'אטים, עריכה, מחיקה
    // ===================================================
    [QueryProperty(nameof(SelectedWorkPlace), "selectedWorkPlace")]
    public partial class JobDetailsViewModel : ObservableObject
    {
        private readonly IWorkPlaceRepository _workPlaceRepository;
        private readonly IAlertService _alertService;
        private readonly IAppLogger _appLogger;

        // אובייקט המשרה שמוצגת — מגיע מ-FindJobView דרך [QueryProperty]
        [ObservableProperty] private WorkPlace? _selectedWorkPlace;

        // האם המשרה במועדפים של המשתמש הנוכחי
        [ObservableProperty] private bool _isFavorite;

        // האם בתהליך — מציג Spinner ומונע לחיצות כפולות
        [ObservableProperty] private bool _isBusy;

        // האם בתהליך טעינת ביקורות
        [ObservableProperty] private bool _isLoadingReviews;

        // האם המשתמש הנוכחי הוא בעל המשרה
        [ObservableProperty] private bool _isCreator;

        // האם המשתמש כבר דיווח על משרה זו
        [ObservableProperty] private bool _hasReported;

        // האם המשרה מאוישת (מלאה)
        [ObservableProperty] private bool _isFilled;

        // ביקורות המשרה — מוצגות ב-CollectionView בתחתית המסך
        public ObservableCollection<Review> Reviews { get; set; } = new();

        // ————— Computed Properties — ערכים המחושבים מנתונים אחרים —————

        // מחרוזת הדירוג — לדוגמה: "4.2 / 5  (12 rated)"
        public string StarRating =>
            SelectedWorkPlace == null ? "—" :
            $"{SelectedWorkPlace.WorkerRating:F1} / 5  ({SelectedWorkPlace.ReviewCount} rated)";

        // טקסט כפתור המועדפים — משתנה לפי מצב IsFavorite
        public string FavoriteButtonText => IsFavorite ? "★ Remove from Favorites" : "☆ Add to Favorites";

        // האם יש ביקורות — שולט על הצגת ההודעה "אין ביקורות עדיין"
        public bool HasReviews => Reviews.Any();

        // היפוך IsCreator — שולט על הצגת כפתורי המועמד (צ'אט, דיווח)
        public bool IsNotCreator => !IsCreator;

        // טקסט כפתור הדיווח — משתנה אחרי דיווח ראשון
        public string ReportButtonText => HasReported ? "⚑ Already Reported" : "⚑ Report this Job";

        // טקסט כפתור המאויש — משתנה לפי מצב IsFilled
        public string FilledButtonText => IsFilled ? "✅ Mark as Open" : "🔒 Mark as Filled (מאויש)";

        public JobDetailsViewModel(IWorkPlaceRepository workPlaceRepository, IAlertService alertService, IAppLogger appLogger)
        {
            _workPlaceRepository = workPlaceRepository;
            _alertService = alertService;
            _appLogger = appLogger;
        }

        // נקרא אוטומטית כש-SelectedWorkPlace מקבל ערך (מגיע מ-FindJobView).
        // מאתחל את כל הנתונים הנוספים: מועדפים, ביקורות, דיווחים, הרשאות.
        partial void OnSelectedWorkPlaceChanged(WorkPlace? value)
        {
            if (value == null) return;

            OnPropertyChanged(nameof(StarRating));   // עדכן תצוגת דירוג
            OnPropertyChanged(nameof(IsNotCreator)); // עדכן הצגת כפתורים

            // בדיקה אם המשתמש הוא בעל המשרה — השוואת מזהים
            IsCreator = (App.Current as App)?.CurrentUser?.Id == value.CreatedByUserId;
            IsFilled = value.IsFilled;

            // טעינת נתונים אסינכרוניים במקביל
            LoadFavoriteStatusAsync(value.Id);
            LoadReviewsAsync(value.Id);
            LoadReportStatusAsync(value.Id);
        }

        // ————— On...Changed: עדכון Computed Properties אוטומטית —————
        // נקראים אוטומטית כשהשדה המתאים משתנה, ומעדכנים את ה-XAML

        partial void OnIsFavoriteChanged(bool value) => OnPropertyChanged(nameof(FavoriteButtonText));
        partial void OnIsCreatorChanged(bool value) => OnPropertyChanged(nameof(IsNotCreator));
        partial void OnHasReportedChanged(bool value) => OnPropertyChanged(nameof(ReportButtonText));
        partial void OnIsFilledChanged(bool value) => OnPropertyChanged(nameof(FilledButtonText));

        // בדיקה אסינכרונית אם המשרה במועדפי המשתמש
        private async void LoadFavoriteStatusAsync(string workPlaceId)
        {
            var userId = (App.Current as App)!.CurrentUser!.Id;
            IsFavorite = await _workPlaceRepository.IsFavoriteAsync(userId, workPlaceId);
        }

        // בדיקה אסינכרונית אם המשתמש כבר דיווח על המשרה
        private async void LoadReportStatusAsync(string workPlaceId)
        {
            var userId = (App.Current as App)!.CurrentUser!.Id;
            HasReported = await _workPlaceRepository.HasUserReportedAsync(workPlaceId, userId);
        }

        // טעינת ביקורות המשרה מ-Firebase
        private async void LoadReviewsAsync(string workPlaceId)
        {
            IsLoadingReviews = true;
            try
            {
                var reviews = await _workPlaceRepository.GetReviewsAsync(workPlaceId);
                Reviews.Clear();
                foreach (var r in reviews)
                    Reviews.Add(r);
                OnPropertyChanged(nameof(HasReviews)); // עדכן האם יש ביקורות להצגה
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"LoadReviewsAsync failed: {ex.Message}");
            }
            finally
            {
                IsLoadingReviews = false;
            }
        }

        // הוספה/הסרה מהמועדפים — מחליף בין שני המצבים
        [RelayCommand]
        private async Task ToggleFavorite()
        {
            if (SelectedWorkPlace == null) return;
            IsBusy = true;
            try
            {
                var userId = (App.Current as App)!.CurrentUser!.Id;
                if (IsFavorite)
                {
                    await _workPlaceRepository.RemoveFromFavoritesAsync(userId, SelectedWorkPlace.Id);
                    IsFavorite = false;
                    await _alertService.ShowAlertAsync("Favorites", $"{SelectedWorkPlace.Name} removed from favorites.", "OK");
                }
                else
                {
                    await _workPlaceRepository.AddToFavoritesAsync(userId, SelectedWorkPlace.Id);
                    IsFavorite = true;
                    await _alertService.ShowAlertAsync("Favorites", $"{SelectedWorkPlace.Name} added to favorites!", "OK");
                }
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"ToggleFavorite failed: {ex.Message}");
                await _alertService.ShowAlertAsync("Error", "Could not update favorites. Please try again.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ניווט לכתיבת ביקורת — שולח ID ושם המשרה
        [RelayCommand]
        private async Task AddReview()
        {
            if (SelectedWorkPlace == null) return;
            var param = new Dictionary<string, object>
            {
                { "workPlaceId", SelectedWorkPlace.Id },
                { "workPlaceName", SelectedWorkPlace.Name ?? string.Empty }
            };
            await Shell.Current.GoToAsync("AddReviewView", param);
        }

        // פתיחת צ'אט עם המנהל (כמועמד).
        // otherUserId ריק — Firebase יזהה את המנהל לפי המשרה.
        [RelayCommand]
        private async Task OpenChat()
        {
            if (SelectedWorkPlace == null) return;
            var param = new Dictionary<string, object>
            {
                { "workPlaceId", SelectedWorkPlace.Id },
                { "workPlaceName", SelectedWorkPlace.Name ?? string.Empty },
                { "otherUserId", string.Empty }, // ריק = הצ'אט של המשתמש הנוכחי עם המנהל
                { "otherUserName", SelectedWorkPlace.CreatedByUserName ?? "Manager" }
            };
            await Shell.Current.GoToAsync("ChatView", param);
        }

        // ניווט לרשימת כל הצ'אטים מהמועמדים (לבעל משרה בלבד)
        [RelayCommand]
        private async Task ViewApplicantChats()
        {
            if (SelectedWorkPlace == null) return;
            var param = new Dictionary<string, object>
            {
                { "workPlaceId", SelectedWorkPlace.Id },
                { "workPlaceName", SelectedWorkPlace.Name ?? string.Empty }
            };
            await Shell.Current.GoToAsync("ChatRoomsView", param);
        }

        // ניווט למסך עריכת המשרה (לבעל משרה בלבד)
        [RelayCommand]
        private async Task EditJob()
        {
            if (SelectedWorkPlace == null) return;
            var param = new Dictionary<string, object> { { "editWorkPlace", SelectedWorkPlace } };
            await Shell.Current.GoToAsync("EditJobView", param);
        }

        // מחיקת המשרה — מבקש אישור לפני
        [RelayCommand]
        private async Task DeleteJob()
        {
            if (SelectedWorkPlace == null) return;
            bool confirm = await Shell.Current.DisplayAlert(
                "Delete Job",
                $"Are you sure you want to delete '{SelectedWorkPlace.Name}'? This cannot be undone.",
                "Yes, Delete", "Cancel");
            if (!confirm) return;

            IsBusy = true;
            try
            {
                await _workPlaceRepository.DeleteWorkPlaceAsync(SelectedWorkPlace.Id);
                await Shell.Current.GoToAsync(".."); // חזרה לרשימת המשרות
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"DeleteJob failed: {ex.Message}");
                await _alertService.ShowAlertAsync("Error", "Could not delete job. Please try again.", "OK");
            }
            finally { IsBusy = false; }
        }

        // דיווח על משרה — פותח שדה קלט לסיבת הדיווח.
        // מגביל לדיווח אחד בלבד לכל משתמש.
        [RelayCommand]
        private async Task ReportJob()
        {
            if (SelectedWorkPlace == null) return;

            // בדיקה שהמשתמש לא דיווח כבר
            if (HasReported)
            {
                await _alertService.ShowAlertAsync("Already Reported", "You have already reported this job.", "OK");
                return;
            }

            // DisplayPromptAsync = חלון עם שדה קלט טקסט חופשי (עד 300 תווים)
            string? reason = await Shell.Current.DisplayPromptAsync(
                "⚑ Report Job",
                $"Why are you reporting '{SelectedWorkPlace.Name}'?\nDescribe the problem (incorrect info, fake listing, etc.)",
                accept: "Send Report",
                cancel: "Cancel",
                placeholder: "e.g. The salary listed is wrong, address doesn't exist...",
                maxLength: 300);

            if (reason == null) return; // המשתמש לחץ Cancel

            if (string.IsNullOrWhiteSpace(reason))
            {
                await _alertService.ShowAlertAsync("Required", "Please write a reason before submitting the report.", "OK");
                return;
            }

            IsBusy = true;
            try
            {
                var currentUser = (App.Current as App)!.CurrentUser!;
                var report = new Model.WorkPlaceReport
                {
                    WorkPlaceId = SelectedWorkPlace.Id,
                    WorkPlaceName = SelectedWorkPlace.Name ?? string.Empty,
                    ReportedByUserId = currentUser.Id,
                    ReporterName = $"{currentUser.FirstName} {currentUser.LastName}".Trim(),
                    Reason = reason.Trim(),
                    ReportDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
                };
                await _workPlaceRepository.ReportWorkPlaceAsync(report);
                HasReported = true; // חוסם דיווח נוסף על אותה משרה
                await _alertService.ShowAlertAsync("Reported", "Thank you. The admin will review this listing.", "OK");
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"ReportJob failed: {ex.Message}");
                await _alertService.ShowAlertAsync("Error", "Could not submit report. Please try again.", "OK");
            }
            finally { IsBusy = false; }
        }

        // שינוי מצב המשרה בין "פנויה" ל"מאוישת" (לבעל משרה בלבד)
        [RelayCommand]
        private async Task ToggleFilled()
        {
            if (SelectedWorkPlace == null) return;
            IsBusy = true;
            try
            {
                var newValue = !IsFilled; // הפיכת המצב הנוכחי
                await _workPlaceRepository.SetWorkPlaceFilledAsync(SelectedWorkPlace.Id, newValue);
                IsFilled = newValue;
                SelectedWorkPlace.IsFilled = newValue; // עדכון האובייקט המקומי
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"ToggleFilled failed: {ex.Message}");
                await _alertService.ShowAlertAsync("Error", "Could not update filled status.", "OK");
            }
            finally { IsBusy = false; }
        }

        // חזרה לרשימת המשרות
        [RelayCommand]
        private async Task BackToFindJob() => await Shell.Current.GoToAsync("..");

        // ניווט למועדפים
        [RelayCommand]
        private async Task GoToFavoriteJobs() => await Shell.Current.GoToAsync("FavoriteJobsView");

        // נקרא כשחוזרים למסך (לדוגמה: אחרי כתיבת ביקורת).
        // מרענן ביקורות, מועדפים וסטטוס דיווח.
        internal void OnAppearing()
        {
            if (SelectedWorkPlace == null) return;
            IsCreator = (App.Current as App)?.CurrentUser?.Id == SelectedWorkPlace.CreatedByUserId;
            IsFilled = SelectedWorkPlace.IsFilled;
            LoadReviewsAsync(SelectedWorkPlace.Id);
            LoadFavoriteStatusAsync(SelectedWorkPlace.Id);
            LoadReportStatusAsync(SelectedWorkPlace.Id);
        }
    }
}
