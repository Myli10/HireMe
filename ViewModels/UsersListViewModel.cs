using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebase.Database.Streaming;
using FirebaseWorkout.Helper;
using FirebaseWorkout.Model;
using FirebaseWorkout.Service;
using FirebaseWorkout.Service.DBService;
using FirebaseWorkout.Service.DBService.Firebase;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirebaseWorkout.ViewModels
{
    // רשימת כל המשתמשים — למנהל בלבד
    // מנוי Firebase בזמן אמת: כשמשתמש נרשם/מעודכן/נמחק — הרשימה מתעדכנת אוטומטית
    public partial class UsersListViewModel : ObservableObject
    {
        private readonly IAppLogger _appLogger;
        private readonly IAlertService _alertService;
        private readonly IAppUserRepository _dbService;

        // "כרטיס מנוי" ל-Firebase — חייב לבטל אותו כשעוזבים את המסך (OnDisappearing)
        IDisposable? _dbSubscription;

        private List<AppUser> _allUsers = new(); // רשימת גיבוי לפני סינון
        public ObservableCollection<AppUser> AllUsers { get; set; }

        [ObservableProperty] private AppUser? _selectedUser;
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _filterIcon;

        // כל שינוי בשדה זה מפעיל FilterBySearch() אוטומטית — חיפוש חי
        [ObservableProperty] private string _searchText;

        public UsersListViewModel(IAlertService alertService, IAppUserRepository dbService, IAppLogger appLogger)
        {
            _appLogger = appLogger;
            _alertService = alertService;
            _dbService = dbService;
            FilterIcon = FontHelper.FILTER_ON_ICON;
            AllUsers = new ObservableCollection<AppUser>();
        }

        [RelayCommand]
        private void ClearFilter()
        {
            SearchText = string.Empty;
            FillUsersList();
        }

        [RelayCommand]
        private void Search() => FilterBySearch();

        // נקרא אוטומטית אחרי כל תו שמוקלד — חיפוש בזמן אמת
        partial void OnSearchTextChanged(string value) => FilterBySearch();

        // מסנן בשם פרטי, שם משפחה ואימייל — לא תלוי רישיות
        private void FilterBySearch()
        {
            AllUsers.Clear();
            var query = SearchText?.Trim().ToLower() ?? string.Empty;
            var filtered = string.IsNullOrEmpty(query)
                ? _allUsers
                : _allUsers.Where(u =>
                    (u.FirstName?.ToLower().Contains(query) ?? false) ||
                    (u.LastName?.ToLower().Contains(query) ?? false) ||
                    (u.UserEmail?.ToLower().Contains(query) ?? false));
            foreach (var user in filtered)
                AllUsers.Add(user);
        }

        // לחיצה על שורה → ניווט לפרופיל המשתמש (מנהל עורך)
        [RelayCommand]
        private async Task NavigateToAccountPage()
        {
            if (SelectedUser != null)
            {
                Dictionary<string, object> param = new Dictionary<string, object>();
                param.Add("selectedUser", SelectedUser);
                await Shell.Current.GoToAsync("AccountView", param);
            }
        }

        // מנוי לשינויים ב-Firebase בזמן אמת
        // InsertOrUpdate = משתמש חדש/עודכן | Delete = משתמש נמחק
        private async Task SubscribeToDbUpdates()
        {
            if (_dbSubscription != null) CancelDbSubscription();

            _dbSubscription = (_dbService as FirebaseUsersRepository)!.SubscribeToUserChanges()
                .Subscribe(item =>
                {
                    // MainThread חובה — Firebase callbacks רצים על Thread נפרד, UI חייב על Main Thread
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (item.EventType == FirebaseEventType.InsertOrUpdate)
                            AddOrUpdateUser(item.Object);
                        else if (item.EventType == FirebaseEventType.Delete)
                            RemoveUser(item.Key);
                        FillUsersList();
                    });
                },
                ex => _appLogger.LogDebug($"Error: {ex.Message}"));
        }

        private void FillUsersList()
        {
            AllUsers.Clear();
            foreach (var user in _allUsers)
                AllUsers.Add(user);
        }

        // אם קיים — מחליף במקומו (שומר סדר), אחרת מוסיף לסוף
        private void AddOrUpdateUser(AppUser item)
        {
            var index = _allUsers.FindIndex(u => u.Id == item.Id);
            if (index != -1)
                _allUsers[index] = item;
            else
                _allUsers.Add(item);
        }

        private void RemoveUser(string userId)
        {
            var item = _allUsers.Where(u => u.Id == userId).FirstOrDefault();
            if (item != null) _allUsers.Remove(item);
        }

        private void CancelDbSubscription()
        {
            _dbSubscription?.Dispose(); // מבטל קבלת עדכונים מ-Firebase
            _dbSubscription = null;
        }

        internal async void OnAppearing()
        {
            _allUsers.Clear();
            await SubscribeToDbUpdates();
            SelectedUser = null!;
        }

        // חובה לבטל מנוי כשעוזבים — אחרת Firebase ממשיך לשלוח נתונים ברקע
        internal void OnDisappearing()
        {
            CancelDbSubscription();
        }
    }
}
