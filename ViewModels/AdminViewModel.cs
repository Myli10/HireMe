using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FirebaseWorkout.ViewModels
{
    // לוח בקרה של המנהל — רק ניווט, אין נתונים לטעון
    public partial class AdminViewModel : ObservableObject
    {
        public AdminViewModel() { }

        [RelayCommand]
        private async Task NavigateToUsersListView()
        {
            await Shell.Current.GoToAsync("UsersListView");
        }

        [RelayCommand]
        private async Task NavigateToReportedJobs()
        {
            await Shell.Current.GoToAsync("ReportedJobsView");
        }
    }
}
