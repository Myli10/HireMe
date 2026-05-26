using FirebaseWorkout.Model;
using FirebaseWorkout.Service.DBService;
using FirebaseWorkout.Service.DBService.Firebase;
using FirebaseWorkout.Views;

namespace FirebaseWorkout
{
    // ===================================================
    // App.xaml.cs — נקודת הכניסה הראשית לאפליקציה
    // ===================================================
    // קובץ זה מגדיר את האובייקט הגלובלי של האפליקציה.
    // כל חלק באפליקציה יכול לגשת ל-App.Current כדי לקבל
    // מידע על המשתמש המחובר כרגע.
    // ===================================================
    public partial class App : Application
    {
        // המשתמש שמחובר כרגע לאפליקציה.
        // null = אין משתמש מחובר (לא עבר התחברות).
        // מתעדכן אחרי SignIn/SignUp ומתאפס אחרי Logout.
        public AppUser? CurrentUser { get; set; } = null;

        private readonly SignInView _signInView;
        private readonly IAuthService _authService;
        private readonly IAppUserRepository _userRepository;

        // הזרקת תלויות (Dependency Injection):
        // .NET MAUI מספק את האובייקטים האלה אוטומטית מה-MauiProgram.cs
        public App(SignInView view, IAuthService authService, IAppUserRepository userRepository)
        {
            InitializeComponent(); // טוען את קובץ App.xaml (צבעים, סגנונות גלובליים)
            _signInView = view;
            _authService = authService;
            _userRepository = userRepository;
        }

        // נקרא כשהאפליקציה נפתחת בפעם הראשונה.
        // מגדיר שהמסך הראשון שיוצג הוא מסך ההתחברות (SignInView).
        // NavigationPage עוטפת את המסך ומאפשרת ניווט קדימה/אחורה.
        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new NavigationPage(_signInView));
        }

        // OnStart — נקרא כשהאפליקציה עולה (כולל לאחר שהייתה ברקע).
        // כרגע ריק — ניתן להוסיף כאן לוגיקת אתחול עתידית.
        protected override void OnStart()
        {
            base.OnStart();
        }
    }
}
