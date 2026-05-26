using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Auth.Repository;
using FirebaseWorkout.Service;
using FirebaseWorkout.Service.DBService;
using FirebaseWorkout.Service.DBService.Firebase;
using Microsoft.Extensions.Logging;

namespace FirebaseWorkout
{
    // ===================================================
    // MauiProgram.cs — הגדרת כל שירותי האפליקציה
    // ===================================================
    // זהו קובץ ההגדרות הראשי של האפליקציה.
    // כאן מגדירים מה יש ב-Dependency Injection (DI) —
    // כלומר, אילו אובייקטים המערכת תיצור ותספק אוטומטית
    // לכל ViewModel ו-View שצריכים אותם.
    //
    // AddTransient = צור אובייקט חדש בכל פעם שמישהו מבקש
    // AddSingleton = צור אובייקט אחד ושתף אותו עם כולם
    // ===================================================
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>() // מגדיר ש-App.xaml.cs הוא נקודת הכניסה
                .ConfigureFonts(fonts =>
                {
                    // פונטים זמינים בכל האפליקציה לפי שם קצר
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons"); // אייקונים (סמלים גרפיים)
                });

            #region רישום Views, ViewModels ו-Services ל-Dependency Injection
            builder.RegisterViews()       // רישום כל המסכים
                   .RegisterViewModels()  // רישום כל ה-ViewModels
                   .RegisterServices();   // רישום כל השירותים (Firebase, Alert, Logger)
            #endregion

#if DEBUG
            // בסביבת פיתוח בלבד — הצג לוגים של שגיאות ב-Output חלון VS
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        // רישום כל המסכים (Views) של האפליקציה.
        // AddTransient = כל פעם שנכנסים למסך נוצר עותק חדש נקי.
        public static MauiAppBuilder RegisterViews(this MauiAppBuilder builder)
        {
            builder.Services.AddTransient<AppShell>();
            builder.Services.AddTransient<Views.SignInView>();
            builder.Services.AddTransient<Views.SignUpView>();
            builder.Services.AddTransient<Views.MainPageView>();
            builder.Services.AddTransient<Views.AdminView>();
            builder.Services.AddTransient<Views.UsersListView>();
            builder.Services.AddTransient<Views.AccountView>();
            builder.Services.AddTransient<Views.FindJobView>();
            builder.Services.AddTransient<Views.JobDetailsView>();
            builder.Services.AddTransient<Views.FavoriteJobsView>();
            builder.Services.AddTransient<Views.AddJobView>();
            builder.Services.AddTransient<Views.AddReviewView>();
            builder.Services.AddTransient<Views.JobMapView>();
            builder.Services.AddTransient<Views.EditJobView>();
            builder.Services.AddTransient<Views.ChatView>();
            builder.Services.AddTransient<Views.ChatRoomsView>();
            builder.Services.AddTransient<Views.MyChatsView>();
            builder.Services.AddTransient<Views.ReportedJobsView>();
            return builder;
        }

        // רישום כל ה-ViewModels (הלוגיקה) של האפליקציה.
        // AddTransient = כל מסך מקבל ViewModel חדש ונקי.
        public static MauiAppBuilder RegisterViewModels(this MauiAppBuilder builder)
        {
            builder.Services.AddTransient<ViewModels.AppShellViewModel>();
            builder.Services.AddTransient<ViewModels.SignInViewModel>();
            builder.Services.AddTransient<ViewModels.SignUpViewModel>();
            builder.Services.AddTransient<ViewModels.MainPageViewModel>();
            builder.Services.AddTransient<ViewModels.AdminViewModel>();
            builder.Services.AddTransient<ViewModels.UsersListViewModel>();
            builder.Services.AddTransient<ViewModels.AccountViewModel>();
            builder.Services.AddTransient<ViewModels.FindJobViewModel>();
            builder.Services.AddTransient<ViewModels.JobDetailsViewModel>();
            builder.Services.AddTransient<ViewModels.FavoriteJobsViewModel>();
            builder.Services.AddTransient<ViewModels.AddJobViewModel>();
            builder.Services.AddTransient<ViewModels.AddReviewViewModel>();
            builder.Services.AddTransient<ViewModels.JobMapViewModel>();
            builder.Services.AddTransient<ViewModels.EditJobViewModel>();
            builder.Services.AddTransient<ViewModels.ChatViewModel>();
            builder.Services.AddTransient<ViewModels.ChatRoomsViewModel>();
            builder.Services.AddTransient<ViewModels.MyChatsViewModel>();
            builder.Services.AddTransient<ViewModels.ReportedJobsViewModel>();
            return builder;
        }

        // רישום כל השירותים של האפליקציה.
        // AddSingleton = אובייקט אחד משותף לכולם (Logger, AlertService, Auth)
        // AddTransient = אובייקט חדש לכל מי שמבקש (Repository)
        public static MauiAppBuilder RegisterServices(this MauiAppBuilder builder)
        {
            builder.Services.AddSingleton<IAppLogger, LogService>();           // לוגר — אחד לכולם
            builder.Services.AddSingleton<IAlertService, AlertService>();      // חלונות התראה — אחד לכולם
            builder.Services.AddSingleton<IAuthService, FirebaseAuthService>(); // אימות Firebase — אחד לכולם
            builder.Services.AddTransient<IAppUserRepository, FirebaseUsersRepository>();     // גישה למשתמשים ב-Firebase
            builder.Services.AddTransient<IWorkPlaceRepository, FirebaseWorkPlaceRepository>(); // גישה למשרות ב-Firebase
            builder.Services.AddTransient<IChatRepository, FirebaseChatRepository>();           // גישה לצ'אטים ב-Firebase
            return builder;
        }
    }
}
