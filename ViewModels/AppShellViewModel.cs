using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FirebaseWorkout.Helper;
using FirebaseWorkout.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FirebaseWorkout.ViewModels
{
    // ===================================================
    // AppShellViewModel — לוגיקת סרגל הניווט הגלובלי
    // ===================================================
    // מנהל את כפתורי ה-Shell (סרגל ניווט עליון/תחתון)
    // הנגישים מכל מסך באפליקציה לאחר ההתחברות.
    // כפתור Admin מוצג רק אם המשתמש הוא מנהל (IsAdmin = true).
    // ===================================================
    public partial class AppShellViewModel : ObservableObject
    {
        private Page _page; // מסך SignIn לחזרה אליו בעת Logout

        // האם המשתמש הוא מנהל — שולט בהצגת כפתור Admin ב-Shell
        [ObservableProperty]
        public bool? _isAdmin = false;

        // קודי אייקונים מ-FontHelper (גופן MaterialIcons)
        [ObservableProperty]
        private string _logoutIcon; // אייקון יציאה

        [ObservableProperty]
        private string _adminIcon; // אייקון מנהל

        [ObservableProperty]
        private string _homeIcon; // אייקון בית

        [ObservableProperty]
        private string _accountIcon; // אייקון פרופיל משתמש

        public AppShellViewModel(SignInView signInView)
        {
            _page = signInView;

            // בדיקה אם המשתמש המחובר הוא מנהל — ישפיע על הצגת כפתור Admin
            _isAdmin = (App.Current as App)!.CurrentUser!.IsAdmin;

            // טעינת קודי אייקונים מ-FontHelper
            _logoutIcon = FontHelper.LOGOUT_ICON;
            _adminIcon = FontHelper.ADMIN_ICON;
            _homeIcon = FontHelper.HOME_ICON;
            _accountIcon = FontHelper.PERSON_ICON;
        }

        // מתנתק מהאפליקציה — מאפס את CurrentUser וחוזר למסך ההתחברות
        [RelayCommand]
        private void Logout()
        {
            (App.Current as App)!.CurrentUser = null; // מחיקת המשתמש המחובר
            Application.Current.Windows[0].Page = new NavigationPage(_page); // חזרה ל-SignIn
        }

        // ניווט ללוח הבקרה של המנהל (AdminView) — גלוי רק למנהלים
        [RelayCommand]
        private async Task NavigateToAdminPage()
        {
            await Shell.Current.GoToAsync("AdminView");
        }

        // ניווט למסך הבית (MainPageView)
        [RelayCommand]
        private async Task NavigateToHomePage()
        {
            await Shell.Current.GoToAsync("MainPageView");
        }

        // ניווט למסך פרופיל המשתמש (AccountView)
        [RelayCommand]
        private async Task NavigateToAccountPage()
        {
            await Shell.Current.GoToAsync("AccountView");
        }
    }
}
