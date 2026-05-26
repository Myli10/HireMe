using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using FirebaseWorkout.Helper;
using FirebaseWorkout.Model;
using FirebaseWorkout.Service.DBService;
using FirebaseWorkout.Service.DBService.Firebase;
using FirebaseWorkout.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FirebaseWorkout.ViewModels
{
    // ===================================================
    // SignUpViewModel — לוגיקת מסך ההרשמה
    // ===================================================
    // מנהל את תהליך יצירת משתמש חדש.
    // הכפתור "הרשמה" מופעל רק כשכל השדות תקינים (Validate).
    // תנאי תקינות:
    //   - שם פרטי + משפחה: לא ריקים
    //   - אימייל: לא ריק
    //   - סיסמה: יותר מ-5 תווים
    //   - טלפון: בדיוק 10 ספרות
    // ===================================================
    public partial class SignUpViewModel : ObservableObject
    {
        private AppUser? newUser;                    // האובייקט החדש שייווצר ב-Firebase
        private readonly IAppUserRepository _dbService; // שירות Firebase

        // ————— שדות הטופס —————
        // כל שדה מפעיל ChangeCanExecute אחרי כל שינוי
        // כדי לבדוק בזמן אמת אם הכפתור צריך להיות פעיל.

        private string _fName;
        private string _lName;
        private string _uEmail;
        private string _uPassword;
        private string _uMobile;

        #region Properties
        public INavigation Navigation { get; set; }

        // שם פרטי — כל שינוי בשדה מעדכן את הכפתור
        public string FName
        {
            get => _fName;
            set
            {
                if (_fName != value)
                {
                    _fName = value;
                    OnPropertyChanged();
                    (SignUpCommand as Command).ChangeCanExecute(); // בדוק אם הכפתור פעיל
                }
            }
        }

        // שם משפחה
        public string LName
        {
            get => _lName;
            set
            {
                if (_lName != value)
                {
                    _lName = value;
                    OnPropertyChanged();
                    (SignUpCommand as Command).ChangeCanExecute();
                }
            }
        }

        // כתובת אימייל
        public string UEmail
        {
            get => _uEmail;
            set
            {
                if (_uEmail != value)
                {
                    _uEmail = value;
                    OnPropertyChanged();
                    (SignUpCommand as Command).ChangeCanExecute();
                }
            }
        }

        // סיסמה — חייבת להיות יותר מ-5 תווים
        public string UPassword
        {
            get => _uPassword;
            set
            {
                if (_uPassword != value)
                {
                    _uPassword = value;
                    OnPropertyChanged();
                    (SignUpCommand as Command).ChangeCanExecute();
                }
            }
        }

        // מספר טלפון — חייב להיות בדיוק 10 ספרות
        public string UMobile
        {
            get => _uMobile;
            set
            {
                if (_uMobile != value)
                {
                    _uMobile = value;
                    OnPropertyChanged();
                    (SignUpCommand as Command).ChangeCanExecute();
                }
            }
        }

        // האם בתהליך הרשמה — מציג Spinner ומונע לחיצות כפולות
        [ObservableProperty]
        private bool _isBusy;

        // אייקון עין (פתוחה/סגורה) לכפתור הצגת/הסתרת סיסמה
        [ObservableProperty]
        private string _passwordIconCode;

        // האם שדה הסיסמה מוסתר
        [ObservableProperty]
        private bool _entryAsPassword;

        // האם להציג הודעת שגיאה
        [ObservableProperty]
        private bool _signUpMessageVisible;

        // תוכן הודעת השגיאה מ-Firebase
        [ObservableProperty]
        private string _errorMessage;

        // פקודת ההרשמה — הכפתור פעיל רק כשכל השדות עוברים Validate()
        public ICommand SignUpCommand { get; }

        #endregion

        public SignUpViewModel(IAppUserRepository dbService)
        {
            _isBusy = false;
            _dbService = dbService;
            _entryAsPassword = true; // ברירת מחדל: הסיסמה מוסתרת
            _passwordIconCode = FontHelper.OPEN_EYE_ICON;

            // SignUpCommand מקושר ל-Validate() כ-canExecute:
            // הכפתור פעיל רק כשהפונקציה מחזירה true
            SignUpCommand = new Command(SignUp, Validate);
        }

        // יוצר משתמש חדש ב-Firebase Auth ו-Database.
        // בהצלחה: שומר כ-CurrentUser ומעבר ל-AppShell.
        private async void SignUp()
        {
            IsBusy = true; // מציג Spinner

            // יוצר אובייקט משתמש חדש עם כל הנתונים מהטופס
            newUser = new AppUser()
            {
                FirstName = FName,
                LastName = LName,
                UserEmail = UEmail,
                UserPassword = UPassword,
                UserMobile = UMobile,
                RegDate = DateTime.Now.ToShortDateString(), // תאריך הרשמה = היום
                UBDate = DateTime.Now.ToShortDateString()   // תאריך לידה (ניתן לעדכון בהמשך)
            };

            try
            {
                // יוצר את המשתמש ב-Firebase ומקבל את ה-ID שנוצר
                newUser.Id = await _dbService!.CreateAsync(newUser);

                // הערה: להפוך משתמש למנהל — בטל את ה-comment בשורות הבאות:
                // await (_dbService as FirebaseUsersRepository)!.SetToAdmin(newUser.Id);
                // newUser.IsAdmin = true;

                IsBusy = false;

                // שמירת המשתמש החדש כמחובר
                (App.Current as App)!.CurrentUser = newUser;

                // מעבר ל-AppShell — מסכי האפליקציה הראשיים
                var mainPage = IPlatformApplication.Current!.Services.GetService<AppShell>();
                Application.Current!.Windows[0].Page = mainPage;
            }
            catch (Exception ex)
            {
                IsBusy = false;
                ShowErrorMessage(ex.Message); // הצג שגיאת Firebase (לדוגמה: "האימייל כבר קיים")
            }
        }

        // מחליף בין הסתרת/הצגת הסיסמה ומעדכן את האייקון
        [RelayCommand]
        private void TogglePassword()
        {
            EntryAsPassword = !EntryAsPassword;
            if (EntryAsPassword)
                PasswordIconCode = FontHelper.OPEN_EYE_ICON;
            else
                PasswordIconCode = FontHelper.CLOSED_EYE_ICON;
        }

        // חזרה למסך ההתחברות (SignInView) — Pop מהסטאק
        [RelayCommand]
        private async Task NavigateToSignIn()
        {
            try
            {
                await Navigation!.PopAsync();
            }
            catch (Exception ex)
            {
                // אם הניווט נכשל — לא עושים כלום
            }
        }

        // בדיקת תקינות כל שדות הטופס.
        // מוחזרת ל-SignUpCommand כ-canExecute — הכפתור פעיל רק אם מחזירה true.
        // נקראת אוטומטית בכל שינוי בשדה.
        private bool Validate()
        {
            var fnameOK = !string.IsNullOrEmpty(FName);                        // שם פרטי לא ריק
            var lnameOK = !string.IsNullOrEmpty(LName);                        // שם משפחה לא ריק
            var emailOK = !string.IsNullOrEmpty(UEmail);                       // אימייל לא ריק
            var passOK = string.IsNullOrEmpty(UPassword) ? false : UPassword.Length > 5;  // סיסמה > 5 תווים
            var mobileOK = string.IsNullOrEmpty(UMobile) ? false : UMobile.Length == 10; // טלפון = 10 ספרות בדיוק

            return fnameOK && lnameOK && emailOK && passOK && mobileOK;
        }

        // מציג את הודעת השגיאה מתחת לטופס
        private void ShowErrorMessage(string message)
        {
            SignUpMessageVisible = true;
            ErrorMessage = message;
        }
    }
}
