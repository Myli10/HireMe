using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FirebaseWorkout.Helper;
using FirebaseWorkout.Service.DBService;
using FirebaseWorkout.Views;
using System.Windows.Input;

namespace FirebaseWorkout.ViewModels
{
    // ===================================================
    // SignInViewModel — לוגיקת מסך ההתחברות
    // ===================================================
    // מנהל את תהליך ההתחברות של משתמש קיים.
    // מאמת אימייל וסיסמה מול Firebase Authentication.
    // כפתור ה-SignIn מופעל רק כשגם האימייל וגם הסיסמה מלאים.
    // ===================================================
    public partial class SignInViewModel : ObservableObject
    {
        private readonly Page _page;             // מסך ה-SignUp לניווט אליו
        private readonly IAppUserRepository _dbService; // שירות Firebase לאימות

        // ————— שדות הטופס —————

        private string _userEmail;
        private string _userPassword;

        // UserEmail ו-UserPassword לא משתמשים ב-[ObservableProperty]
        // כי צריך לקרוא ל-ChangeCanExecute אחרי כל שינוי —
        // זה מעדכן את הכפתור (מופעל/מנוטרל) בזמן הקלדה.
        public string UserEmail
        {
            get => _userEmail;
            set
            {
                if (_userEmail != value)
                {
                    _userEmail = value;
                    OnPropertyChanged(); // מודיע ל-XAML שהערך השתנה
                    (SignInCommand as Command)?.ChangeCanExecute(); // בדוק אם הכפתור צריך להיות פעיל
                }
            }
        }

        public string UserPassword
        {
            get => _userPassword;
            set
            {
                if (_userPassword != value)
                {
                    _userPassword = value;
                    OnPropertyChanged();
                    (SignInCommand as Command)?.ChangeCanExecute(); // בדוק שוב אם הכפתור פעיל
                }
            }
        }

        // קוד האייקון לכפתור הצגת/הסתרת סיסמה (עין פתוחה/סגורה)
        [ObservableProperty] private string _passwordIconCode;

        // האם שדה הסיסמה מוסתר (IsPassword=true = מוסתר עם נקודות)
        [ObservableProperty] private bool _entryAsPassword;

        // האם להציג את הודעת השגיאה
        [ObservableProperty] private bool _signInMessageVisible;

        // תוכן הודעת השגיאה (לדוגמה: "Wrong password")
        [ObservableProperty] private string _errorMessage;

        // האם תהליך ההתחברות בעיצומו — מציג Spinner ומונע לחיצות כפולות
        [ObservableProperty] private bool _isBusy;

        // Navigation לשימוש בניווט לדף ההרשמה (NavigationPage.PushAsync)
        public INavigation Navigation { get; set; }

        // פקודת ההתחברות — מופעלת רק כשגם אימייל וגם סיסמה מלאים (canExecute)
        public ICommand SignInCommand { get; }

        public SignInViewModel(SignUpView view, IAppUserRepository dbService)
        {
            _userEmail = string.Empty;
            _userPassword = string.Empty;
            _page = view;
            _isBusy = false;
            _dbService = dbService;
            _entryAsPassword = true; // ברירת מחדל: הסיסמה מוסתרת
            _passwordIconCode = FontHelper.OPEN_EYE_ICON;

            // canExecute: הכפתור פעיל רק כשגם האימייל וגם הסיסמה אינם ריקים
            SignInCommand = new Command(SignIn, () =>
                !(string.IsNullOrEmpty(UserEmail) || string.IsNullOrEmpty(UserPassword)));
        }

        // מבצע התחברות ל-Firebase Auth.
        // בהצלחה: שומר את המשתמש ב-App.CurrentUser ועובר ל-AppShell (מסכים הראשיים).
        // בכישלון: מציג הודעת שגיאה (לדוגמה: "אימייל לא קיים", "סיסמה שגויה").
        private async void SignIn()
        {
            IsBusy = true;
            try
            {
                // מנסה להתחבר דרך Firebase Authentication
                var user = await _dbService.SignInAsync(UserEmail!, UserPassword!);
                IsBusy = false;

                // שמירת המשתמש המחובר — נגיש מכל מסך דרך (App.Current as App)!.CurrentUser
                (App.Current as App)!.CurrentUser = user;

                // מעבר ל-AppShell — מסכי האפליקציה הראשיים (לאחר התחברות)
                var mainPage = IPlatformApplication.Current!.Services.GetService<AppShell>();
                Application.Current!.Windows[0].Page = mainPage;
            }
            catch (Exception ex)
            {
                IsBusy = false;
                ShowErrorMessage(ex.Message); // מציג את שגיאת Firebase למשתמש
            }
        }

        // מחליף בין הסתרת/הצגת הסיסמה ומעדכן את האייקון בהתאם
        [RelayCommand]
        private void TogglePassword()
        {
            EntryAsPassword = !EntryAsPassword;
            PasswordIconCode = EntryAsPassword ? FontHelper.OPEN_EYE_ICON : FontHelper.CLOSED_EYE_ICON;
        }

        // מנווט למסך ההרשמה (SignUpView)
        [RelayCommand]
        private async Task NavigateToSignUp()
        {
            try { await Navigation!.PushAsync(_page); }
            catch { }
        }

        // מציג את הודעת השגיאה מתחת לטופס
        private void ShowErrorMessage(string message)
        {
            SignInMessageVisible = true;
            ErrorMessage = message;
        }
    }
}
