using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FirebaseWorkout.Model;
using FirebaseWorkout.Service;
using FirebaseWorkout.Service.DBService;

namespace FirebaseWorkout.ViewModels
{
    // ===================================================
    // EditJobViewModel — לוגיקת מסך עריכת משרה קיימת
    // ===================================================
    // [QueryProperty] = מקבל את אובייקט המשרה לעריכה מ-JobDetailsView.
    // שדות שניתן לשנות: תיאור, טלפון, שכר, שעות משמרת, שעות פתיחה, קטגוריה.
    // שדות שלא ניתן לשנות: שם המשרה, כתובת, עיר (יש לכך סיבה עסקית).
    // ===================================================
    [QueryProperty(nameof(EditWorkPlace), "editWorkPlace")]
    public partial class EditJobViewModel : ObservableObject
    {
        private readonly IWorkPlaceRepository _workPlaceRepository;
        private readonly IAlertService _alertService;
        private readonly IAppLogger _appLogger;

        // אובייקט המשרה לעריכה — מגיע מ-JobDetailsView
        [ObservableProperty] private WorkPlace? _editWorkPlace;

        // שדות הניתנים לעריכה
        [ObservableProperty] private string _description = string.Empty;   // תיאור המשרה
        [ObservableProperty] private string _managerPhone = string.Empty;  // טלפון מנהל
        [ObservableProperty] private string _salaryText = string.Empty;    // שכר לשעה (טקסט)
        [ObservableProperty] private string _shiftHours = string.Empty;    // שעות משמרת
        [ObservableProperty] private string _openingHours = string.Empty;  // שעות פתיחה
        [ObservableProperty] private string _category = string.Empty;      // קטגוריה
        [ObservableProperty] private string _customCategory = string.Empty; // קטגוריה מותאמת
        [ObservableProperty] private bool _showCustomCategory;             // האם להציג שדה קטגוריה חופשית
        [ObservableProperty] private bool _isBusy;                         // האם בתהליך שמירה

        // קטגוריות קבועות — זהה לרשימה ב-AddJobViewModel
        public List<string> Categories { get; } = new()
        {
            "מזון ומסעדות", "קמעונאות", "בתי קפה", "סופרמרקט",
            "טכנולוגיה", "משלוחים", "ניקיון", "אבטחה", "אחר"
        };

        // נקרא אוטומטית כש-Category משתנה.
        // מציג/מסתיר שדה קטגוריה חופשית בהתאם.
        partial void OnCategoryChanged(string value)
        {
            ShowCustomCategory = value == "אחר";
            if (!ShowCustomCategory) CustomCategory = string.Empty;
        }

        public EditJobViewModel(IWorkPlaceRepository workPlaceRepository, IAlertService alertService, IAppLogger appLogger)
        {
            _workPlaceRepository = workPlaceRepository;
            _alertService = alertService;
            _appLogger = appLogger;
        }

        // נקרא אוטומטית כש-EditWorkPlace מקבל ערך (מגיע מ-JobDetailsView).
        // ממלא את כל שדות הטופס בנתוני המשרה הקיימת.
        partial void OnEditWorkPlaceChanged(WorkPlace? value)
        {
            if (value == null) return;

            // מילוי שדות הטופס מהמשרה הקיימת
            Description = value.Description ?? string.Empty;
            ManagerPhone = value.ManagerPhone ?? string.Empty;
            SalaryText = value.SalaryPerHour > 0 ? value.SalaryPerHour.ToString() : string.Empty;
            ShiftHours = value.ShiftHours ?? string.Empty;
            OpeningHours = value.OpeningHours ?? string.Empty;

            // זיהוי חכם של קטגוריה:
            // אם הקטגוריה הקיימת לא נמצאת ברשימה הקבועה — היא קטגוריה מותאמת ("אחר")
            var knownCategories = new HashSet<string>(Categories);
            if (!string.IsNullOrWhiteSpace(value.Category) && !knownCategories.Contains(value.Category))
            {
                Category = "אחר";               // בחר "אחר" ב-Picker
                CustomCategory = value.Category; // הצג את הקטגוריה המותאמת בשדה החופשי
            }
            else
            {
                Category = value.Category ?? string.Empty; // קטגוריה רגילה מהרשימה
            }
        }

        // שמירת השינויים ב-Firebase.
        // מעדכן רק את השדות הניתנים לשינוי — לא שם/כתובת.
        [RelayCommand]
        private async Task SaveChanges()
        {
            if (EditWorkPlace == null) return;

            // בדיקת תקינות לפני שמירה
            var error = Validate();
            if (error != null)
            {
                await _alertService.ShowAlertAsync("Invalid Input", error, "OK");
                return;
            }

            double.TryParse(SalaryText, out double salary);
            IsBusy = true;
            try
            {
                // עדכון האובייקט המקומי לפני שמירה ל-Firebase
                EditWorkPlace.Description = Description.Trim();
                EditWorkPlace.ManagerPhone = ManagerPhone.Trim();
                EditWorkPlace.SalaryPerHour = salary;
                EditWorkPlace.ShiftHours = ShiftHours.Trim();
                EditWorkPlace.OpeningHours = OpeningHours.Trim();

                // קביעת הקטגוריה הסופית (מותאמת או קבועה)
                EditWorkPlace.Category = Category == "אחר" && !string.IsNullOrWhiteSpace(CustomCategory)
                    ? CustomCategory.Trim()
                    : (string.IsNullOrWhiteSpace(Category) ? "אחר" : Category.Trim());

                await _workPlaceRepository.EditWorkPlaceAsync(EditWorkPlace); // שמירה ב-Firebase
                await _alertService.ShowAlertAsync("Success", "Job details updated successfully!", "OK");
                await Shell.Current.GoToAsync(".."); // חזרה לפרטי המשרה
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"SaveChanges failed: {ex.Message}");
                await _alertService.ShowAlertAsync("Error", "Could not save changes. Please try again.", "OK");
            }
            finally { IsBusy = false; }
        }

        // ביטול — חזרה לפרטי המשרה ללא שמירה
        [RelayCommand]
        private async Task Cancel() => await Shell.Current.GoToAsync("..");

        // בדיקת תקינות שדות הטופס.
        // מחזירה null אם הכל תקין, אחרת הודעת שגיאה.
        private string? Validate()
        {
            if (string.IsNullOrWhiteSpace(Description) || Description.Trim().Length < 20)
                return "Description must be at least 20 characters.";

            if (!double.TryParse(SalaryText, out double salary) || salary == 0)
                return "Salary cannot be 0 ₪. Please enter the actual hourly wage.";

            // שכר מינימום: 32 ₪ לשעה
            if (salary < 32)
                return "Salary cannot be below the minimum wage (32 ₪/hour).";

            if (string.IsNullOrWhiteSpace(ShiftHours))
                return "Please fill in shift hours (e.g. 09:00 - 17:00).";

            // בדיקת פורמט טלפון — אם הוזן
            if (!string.IsNullOrWhiteSpace(ManagerPhone))
            {
                var phone = ManagerPhone.Replace("-", "").Replace(" ", "");
                if (phone.Length != 10 || !phone.StartsWith("05"))
                    return "Phone number must be 10 digits and start with 05.";
            }

            return null; // הכל תקין
        }
    }
}
