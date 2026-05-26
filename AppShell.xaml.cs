using FirebaseWorkout.ViewModels;
using FirebaseWorkout.Views;

namespace FirebaseWorkout
{
    // ===================================================
    // AppShell.xaml.cs — מנהל הניווט של האפליקציה
    // ===================================================
    // Shell הוא מבנה הניווט הראשי של MAUI.
    // כאן רושמים את כל המסכים (Routes) כדי שניתן יהיה
    // לנווט אליהם עם Shell.Current.GoToAsync("שם-מסך").
    // ===================================================
    public partial class AppShell : Shell
    {
        public AppShell(AppShellViewModel vm)
        {
            InitializeComponent(); // טוען את AppShell.xaml (סרגל תחתון, תפריטים)
            BindingContext = vm;   // מחבר את ה-ViewModel לניווט ולכפתורי ה-Shell

            // רישום כל מסכי האפליקציה לניווט.
            // Routing.RegisterRoute = "שם" → "סוג המסך"
            // אחרי הרישום, GoToAsync("FindJobView") יפתח את FindJobView.
            Routing.RegisterRoute(nameof(MainPageView), typeof(MainPageView));
            Routing.RegisterRoute(nameof(AdminView), typeof(AdminView));
            Routing.RegisterRoute(nameof(AccountView), typeof(AccountView));
            Routing.RegisterRoute(nameof(UsersListView), typeof(UsersListView));
            Routing.RegisterRoute(nameof(FindJobView), typeof(FindJobView));
            Routing.RegisterRoute(nameof(JobDetailsView), typeof(JobDetailsView));
            Routing.RegisterRoute(nameof(FavoriteJobsView), typeof(FavoriteJobsView));
            Routing.RegisterRoute(nameof(AddJobView), typeof(AddJobView));
            Routing.RegisterRoute(nameof(AddReviewView), typeof(AddReviewView));
            Routing.RegisterRoute(nameof(JobMapView), typeof(JobMapView));
            Routing.RegisterRoute(nameof(EditJobView), typeof(EditJobView));
            Routing.RegisterRoute(nameof(ChatView), typeof(ChatView));
            Routing.RegisterRoute(nameof(ChatRoomsView), typeof(ChatRoomsView));
            Routing.RegisterRoute(nameof(MyChatsView), typeof(MyChatsView));
            Routing.RegisterRoute(nameof(ReportedJobsView), typeof(ReportedJobsView));
        }
    }
}
