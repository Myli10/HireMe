using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FirebaseWorkout.Model;
using FirebaseWorkout.Service;
using FirebaseWorkout.Service.DBService;
using System.Collections.ObjectModel;

namespace FirebaseWorkout.ViewModels
{
    // ===================================================
    // FindJobViewModel — לוגיקת מסך חיפוש עבודה
    // ===================================================
    // המסך המרכזי של האפליקציה — מציג רשימת כל המשרות
    // עם אפשרות סינון לפי שכר, קטגוריה ועיר.
    //
    // שתי רשימות:
    //   _allJobs   = כל המשרות מ-Firebase (לא מוצגת ישירות)
    //   WorkPlaces = המשרות שמוצגות כרגע (אחרי סינון)
    // ===================================================
    public partial class FindJobViewModel : ObservableObject
    {
        private readonly IWorkPlaceRepository _workPlaceRepository;
        private readonly IAlertService _alertService;
        private readonly IAppLogger _appLogger;

        // רשימת גיבוי — כל המשרות מ-Firebase.
        // כשמסננים, עוברים על _allJobs — לא על WorkPlaces.
        // כך אפשר לנקות סינון ולחזור לרשימה המלאה.
        private List<WorkPlace> _allJobs = new();

        // הרשימה המוצגת על המסך (מחוברת ל-CollectionView ב-XAML)
        public ObservableCollection<WorkPlace> WorkPlaces { get; set; } = new();

        // קטגוריות לבחירה ב-Picker — מחושבות מהמשרות שנטענו
        public ObservableCollection<string> AvailableCategories { get; } = new();

        // האם בתהליך טעינה — מציג Spinner בראש המסך
        [ObservableProperty] private bool _isBusy;

        // האם לוח הסינון המתקפל מוצג
        [ObservableProperty] private bool _showFilter;

        // שדות הסינון
        [ObservableProperty] private string _minSalaryText = string.Empty; // שכר מינימום
        [ObservableProperty] private string _maxSalaryText = string.Empty; // שכר מקסימום
        [ObservableProperty] private object? _selectedCategory;            // קטגוריה נבחרת
        [ObservableProperty] private string _cityFilter = string.Empty;    // עיר/כתובת

        public FindJobViewModel(IWorkPlaceRepository workPlaceRepository, IAlertService alertService, IAppLogger appLogger)
        {
            _workPlaceRepository = workPlaceRepository;
            _alertService = alertService;
            _appLogger = appLogger;
        }

        // מנווט למסך פרטי המשרה — שולח את אובייקט המשרה כפרמטר
        [RelayCommand]
        private async Task GoToDetails(WorkPlace workPlace)
        {
            if (workPlace == null) return;
            await Shell.Current.GoToAsync("JobDetailsView", new Dictionary<string, object>
            {
                { "selectedWorkPlace", workPlace } // נקלט ב-JobDetailsViewModel דרך [QueryProperty]
            });
        }

        // מציג/מסתיר את לוח הסינון המתקפל
        [RelayCommand]
        private void ToggleFilter()
        {
            ShowFilter = !ShowFilter;
        }

        // מסנן את הרשימה המוצגת לפי שכר, קטגוריה ועיר.
        // עובד על _allJobs (המלאה) — לא על WorkPlaces (המוצגת).
        [RelayCommand]
        private void ApplyFilter()
        {
            double min = 0, max = double.MaxValue;

            // המרת טקסט שכר למספר — אם ריק, ערך ברירת מחדל (0 / אינסוף)
            if (!string.IsNullOrWhiteSpace(MinSalaryText) && double.TryParse(MinSalaryText, out double parsedMin))
                min = parsedMin;
            if (!string.IsNullOrWhiteSpace(MaxSalaryText) && double.TryParse(MaxSalaryText, out double parsedMax))
                max = parsedMax;

            var city = CityFilter.Trim().ToLower(); // חיפוש עיר — לא תלוי רישיות
            var cat = SelectedCategory as string;

            // LINQ: שלושה שלבי סינון
            var filtered = _allJobs
                .Where(j => j.SalaryPerHour >= min && j.SalaryPerHour <= max)               // 1. סינון שכר
                .Where(j => string.IsNullOrEmpty(cat) || cat == "הכל" || j.Category == cat) // 2. סינון קטגוריה
                .Where(j => string.IsNullOrEmpty(city) || (j.Address?.ToLower().Contains(city) ?? false)) // 3. סינון עיר
                .ToList();

            // עדכון הרשימה המוצגת עם התוצאות המסוננות
            WorkPlaces.Clear();
            foreach (var job in filtered)
                WorkPlaces.Add(job);
        }

        // ניקוי כל הסינונים — מציג מחדש את כל המשרות
        [RelayCommand]
        private void ClearFilter()
        {
            MinSalaryText = string.Empty;
            MaxSalaryText = string.Empty;
            SelectedCategory = null;
            CityFilter = string.Empty;

            WorkPlaces.Clear();
            foreach (var job in _allJobs) // חזרה מ-_allJobs המלאה
                WorkPlaces.Add(job);
        }

        // יציאה מהאפליקציה — מאפס CurrentUser וחוזר ל-SignInView
        [RelayCommand]
        private async Task LogOut()
        {
            bool confirm = await Shell.Current.DisplayAlert("Log Out", "Are you sure you want to log out?", "Yes", "No");
            if (!confirm) return;

            (App.Current as App)!.CurrentUser = null;
            Application.Current!.MainPage = App.Current!.Handler!.MauiContext!.Services
                .GetService<Views.SignInView>();
        }

        // ניווט לפרסום משרה חדשה
        [RelayCommand]
        private async Task GoToAddJob() => await Shell.Current.GoToAsync("AddJobView");

        // ניווט למפת המשרות
        [RelayCommand]
        private async Task GoToMap() => await Shell.Current.GoToAsync("JobMapView");

        // ניווט לצ'אטים שלי (כמועמד)
        [RelayCommand]
        private async Task GoToMyChats() => await Shell.Current.GoToAsync("MyChatsView");

        // רענון ידני של הרשימה מ-Firebase
        [RelayCommand]
        private async Task Refresh() => await LoadWorkPlacesAsync();

        // נקרא כשהמסך מופיע — טוען מחדש את המשרות
        internal async void OnAppearing() => await LoadWorkPlacesAsync();

        // טוען כל המשרות מ-Firebase ובונה את רשימת הקטגוריות.
        private async Task LoadWorkPlacesAsync()
        {
            IsBusy = true;
            try
            {
                var jobs = await _workPlaceRepository.GetAllWorkPlacesAsync();
                _allJobs = jobs; // שמירת הרשימה המלאה לסינון עתידי

                WorkPlaces.Clear();
                foreach (var job in jobs)
                    WorkPlaces.Add(job);

                // בניית רשימת קטגוריות ייחודיות למיון אלפביתי
                // Select = לקחת רק Category מכל משרה
                // Distinct = ללא כפילויות
                // OrderBy = מיון אלפביתי
                AvailableCategories.Clear();
                AvailableCategories.Add("הכל"); // אפשרות לאפס סינון קטגוריה
                foreach (var cat in jobs
                    .Select(j => j.Category ?? string.Empty)
                    .Where(c => !string.IsNullOrEmpty(c))
                    .Distinct()
                    .OrderBy(c => c))
                {
                    AvailableCategories.Add(cat);
                }

                // איפוס שדות סינון אחרי כל טעינה מחדש
                MinSalaryText = string.Empty;
                MaxSalaryText = string.Empty;
                SelectedCategory = null;
                CityFilter = string.Empty;
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"LoadWorkPlacesAsync failed: {ex.Message}");
                await _alertService.ShowAlertAsync("Error", "Could not load jobs. Please try again.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
