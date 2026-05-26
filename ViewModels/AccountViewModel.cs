using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using FirebaseWorkout.Helper;
using FirebaseWorkout.Model;
using FirebaseWorkout.Service;
using FirebaseWorkout.Service.DBService;
using FirebaseWorkout.Service.DBService.Firebase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirebaseWorkout.ViewModels
{
    // ===================================================
    // AccountViewModel — לוגיקת מסך פרופיל המשתמש
    // ===================================================
    // משמש לשני מקרים:
    //   1. משתמש רגיל — צופה ועורך את הפרופיל שלו עצמו
    //   2. מנהל (Admin) — צופה ועורך פרופיל של כל משתמש אחר
    //
    // IQueryAttributable — ממשק שמאפשר קבלת פרמטרים בניווט.
    // אם מגיע "selectedUser" — זה מנהל שנכנס לפרופיל של מישהו אחר.
    // אם לא מגיע פרמטר — זה המשתמש שצופה בפרופיל שלו.
    // ===================================================
    public partial class AccountViewModel : ObservableObject, IQueryAttributable
    {
        IAlertService _alertService;
        private readonly IAppUserRepository _dbService; // גישה ל-Firebase

        #region שדות הפרופיל
        [ObservableProperty]
        private string _firstName = string.Empty; // שם פרטי

        [ObservableProperty]
        private string _lastName = string.Empty; // שם משפחה

        [ObservableProperty]
        private string _userEmail = string.Empty; // אימייל (לא ניתן לעריכה)

        [ObservableProperty]
        private string _userMobile = string.Empty; // מספר טלפון

        // המשתמש שהמנהל נכנס לצפות בו (null = המשתמש הנוכחי)
        [ObservableProperty]
        private AppUser? _recievedUser;

        // האם להציג כפתור מחיקה — רק כשמנהל צופה במשתמש אחר (לא עצמו)
        [ObservableProperty]
        private bool _isDeleteButtonVisible;

        // אייקון סמל המחיקה (MaterialIcon)
        [ObservableProperty]
        private string _deleteIcon = string.Empty;

        // האם להציג את תיבת הודעת השגיאה
        [ObservableProperty]
        private bool _errorMessageIsVisible;

        // תוכן הודעת השגיאה (לדוגמה: "שם קצר מדי")
        [ObservableProperty]
        private string _errorMessage = string.Empty;

        // האם בתהליך — מציג Spinner ומנע לחיצות כפולות
        [ObservableProperty]
        private bool _isBusy;

        // תמונת הפרופיל — נטענת מ-Base64 שנשמר ב-Firebase
        [ObservableProperty]
        private ImageSource? _userImageSource;

        // true כשיש תמונה — שולט בנראות שכבות ה-XAML (תמונה / אייקון ברירת מחדל)
        public bool HasProfileImage => UserImageSource != null;

        // נקרא אוטומטית ע"י CommunityToolkit כשUserImageSource משתנה — מעדכן את HasProfileImage
        partial void OnUserImageSourceChanged(ImageSource? value) =>
            OnPropertyChanged(nameof(HasProfileImage));
        #endregion

        public AccountViewModel(IAppUserRepository dbService, IAlertService alertService)
        {
            _alertService = alertService;
            _dbService = dbService;
            DeleteIcon = FontHelper.DELETE_USER_ICON;
            IsDeleteButtonVisible = false; // כפתור מחיקה מוסתר כברירת מחדל
        }

        // מחיקת המשתמש הנבחר מ-Firebase (זמין למנהל בלבד).
        // מבקש אישור לפני המחיקה.
        [RelayCommand]
        private async Task Delete()
        {
            // בקשת אישור ממנהל לפני מחיקה
            bool confirm = await Shell.Current.DisplayAlert(
                "Admin",
                "Are you sure you want to delete this user?",
                "Yes",
                "No"
            );

            if (confirm) // המנהל אישר — מחק מ-Firebase
            {
                try
                {
                    IsBusy = true;
                    await _dbService.DeleteAsync(RecievedUser!); // מחיקה מ-Firebase
                    await Shell.Current.GoToAsync(".."); // חזרה למסך הקודם (רשימת המשתמשים)
                    IsBusy = false;
                }
                catch (Exception ex)
                {
                    IsBusy = false;
                    await _alertService.ShowAlertAsync("KASATA", ex.Message, "OK");
                }
            }
        }

        // עדכון פרטי המשתמש ב-Firebase.
        // מעדכן: שם פרטי, שם משפחה, וטלפון (לא ניתן לשנות אימייל).
        [RelayCommand]
        private async Task Update()
        {
            ErrorMessageIsVisible = false;

            // בדיקת תקינות השדות לפני שליחה ל-Firebase
            if (!Validate())
            {
                await _alertService.ShowAlertAsync("KASATA", ErrorMessage, "OK");
                return;
            }

            AppUser? user = null;

            // אם מנהל עורך משתמש אחר — עדכן אותו. אחרת — עדכן את המשתמש הנוכחי.
            if (RecievedUser != null)
                user = RecievedUser;
            else
                user = (App.Current as App)!.CurrentUser!;

            IsBusy = true;
            try
            {
                // עדכון הנתונים באובייקט ושליחה ל-Firebase
                user.FirstName = FirstName;
                user.LastName = LastName;
                user.UserMobile = UserMobile;

                await _dbService.UpdateAsync(user);
                IsBusy = false;

                await _alertService.ShowAlertAsync("KASATA", "User details updated successfully!", "OK");
            }
            catch (Exception ex)
            {
                IsBusy = false;
                await _alertService.ShowAlertAsync("KASATA", $"Error updating user details: {ex.Message}", "OK");
            }
        }

        // בחירת תמונה מהגלריה, המרה ל-Base64 ושמירה ב-Firebase.
        // גודל מקסימלי: 1.5MB — Firebase Realtime Database מוגבל בגודל נתונים.
        [RelayCommand]
        private async Task GetUserImage()
        {
            try
            {
                // פתיחת חלון בחירת קובץ — מסנן רק תמונות
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select profile photo",
                    FileTypes = FilePickerFileType.Images
                });
                if (result == null) return; // המשתמש ביטל את הבחירה

                IsBusy = true;

                // קריאת התמונה כמערך בתים (bytes)
                var bytes = await File.ReadAllBytesAsync(result.FullPath);

                // בדיקת גודל — מעל 1.5MB יגרום לבעיות ב-Firebase
                if (bytes.Length > 1_500_000)
                {
                    await _alertService.ShowAlertAsync("Image Too Large", "Please pick an image smaller than 1.5 MB.", "OK");
                    return;
                }

                // המרת הבתים למחרוזת Base64 — ניתנת לשמירה ב-Firebase Database
                var base64 = Convert.ToBase64String(bytes);

                // קביעת המשתמש שיש לעדכן (מנהל עורך אחר / משתמש עצמי)
                var user = RecievedUser ?? (App.Current as App)!.CurrentUser!;
                user.ProfileImageBase64 = base64;

                // שמירה ב-Firebase
                await _dbService.UpdateAsync(user);

                // הצגת התמונה מיד על המסך (ממחרוזת bytes, לא מהשרת)
                UserImageSource = ImageSource.FromStream(() => new MemoryStream(bytes));

                await _alertService.ShowAlertAsync("Success", "Profile photo updated!", "OK");
            }
            catch (Exception ex)
            {
                await _alertService.ShowAlertAsync("Error", $"Could not save photo: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // מימוש IQueryAttributable — מקבל פרמטרים מניווט.
        // נקרא אוטומטית כשמגיעים למסך עם פרמטרים.
        // מקרה 1: הגיע "selectedUser" (מנהל פתח פרופיל של משתמש אחר)
        // מקרה 2: לא הגיע פרמטר (משתמש רגיל פתח את הפרופיל שלו)
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            RecievedUser = query.ContainsKey("selectedUser") ? (AppUser)query["selectedUser"] : null;

            if (RecievedUser != null) // מנהל פתח פרופיל של משתמש אחר
            {
                LoadUserDetails(RecievedUser);
                // הצג כפתור מחיקה רק אם המנהל לא מסתכל על עצמו
                IsDeleteButtonVisible = RecievedUser.Id != (App.Current as App)!.CurrentUser!.Id;
            }
            else // משתמש רגיל פתח את הפרופיל שלו
            {
                LoadUserDetails((App.Current as App)!.CurrentUser!);
            }
        }

        // טוען את פרטי המשתמש לשדות הטופס על המסך.
        // ממיר את תמונת הפרופיל מ-Base64 ל-ImageSource להצגה.
        private void LoadUserDetails(AppUser user)
        {
            FirstName = user.FirstName!;
            LastName = user.LastName!;
            UserEmail = user.UserEmail!;
            UserMobile = user.UserMobile ?? string.Empty;

            // אם יש תמונת פרופיל — המר מ-Base64 ל-ImageSource והצג
            if (!string.IsNullOrEmpty(user.ProfileImageBase64))
            {
                var bytes = Convert.FromBase64String(user.ProfileImageBase64); // Base64 → בתים
                UserImageSource = ImageSource.FromStream(() => new MemoryStream(bytes)); // בתים → תמונה
            }
            else
            {
                UserImageSource = null; // אין תמונה — תוצג תמונת ברירת מחדל מה-XAML
            }
        }

        #region Validation Methods — בדיקות תקינות שדות
        // בדיקה כוללת של כל השדות — מחזירה true אם הכל תקין
        private bool Validate()
        {
            var firstNameValid = ValidUserFirstName();
            var lastNameValid = ValidUserLastName();
            var mobileValid = ValidMobile();

            return IsEmptyValidate() && firstNameValid && lastNameValid && mobileValid;
        }

        // בדיקה שאין שדות ריקים
        private bool IsEmptyValidate()
        {
            return !(string.IsNullOrWhiteSpace(FirstName) ||
                   string.IsNullOrWhiteSpace(LastName) ||
                   string.IsNullOrWhiteSpace(UserMobile));
        }

        // שם פרטי חייב להיות לפחות 2 תווים
        private bool ValidUserFirstName()
        {
            if (FirstName.Length < 2)
            {
                ErrorMessage = "First name too short!";
                return false;
            }
            return true;
        }

        // שם משפחה חייב להיות לפחות 2 תווים
        private bool ValidUserLastName()
        {
            if (LastName.Length < 2)
            {
                ErrorMessage = "Last name too short!";
                return false;
            }
            return true;
        }

        // טלפון חייב להיות בדיוק 10 ספרות
        private bool ValidMobile()
        {
            if (UserMobile!.Length != 10)
            {
                ErrorMessage = "Mobile must be between 10 and 15 characters long!";
                return false;
            }
            return true;
        }
        #endregion
    }
}
