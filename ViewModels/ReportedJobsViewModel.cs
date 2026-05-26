using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FirebaseWorkout.Model;
using FirebaseWorkout.Service;
using FirebaseWorkout.Service.DBService;
using System.Collections.ObjectModel;

namespace FirebaseWorkout.ViewModels
{
    // מסך מנהל — ניהול משרות מדווחות
    // Dismiss = מחק דיווחים בלבד (משרה נשארת)
    // DeleteJob = מחק הכל (משרה + דיווחים)
    public partial class ReportedJobsViewModel : ObservableObject
    {
        private readonly IWorkPlaceRepository _workPlaceRepository;
        private readonly IAlertService _alertService;
        private readonly IAppLogger _appLogger;

        [ObservableProperty] private bool _isBusy;

        public ObservableCollection<ReportedJobSummary> ReportedJobs { get; } = new();

        // Computed Properties — לא מתעדכנים אוטומטית, דורשים OnPropertyChanged ידני
        public bool HasReportedJobs => ReportedJobs.Any();
        public bool HasNoReportedJobs => !ReportedJobs.Any();

        public ReportedJobsViewModel(IWorkPlaceRepository workPlaceRepository, IAlertService alertService, IAppLogger appLogger)
        {
            _workPlaceRepository = workPlaceRepository;
            _alertService = alertService;
            _appLogger = appLogger;
        }

        internal async void OnAppearing() => await LoadReportsAsync();

        private async Task LoadReportsAsync()
        {
            IsBusy = true;
            try
            {
                var summaries = await _workPlaceRepository.GetAllReportedJobsAsync();
                ReportedJobs.Clear();
                foreach (var s in summaries)
                    ReportedJobs.Add(s);
                OnPropertyChanged(nameof(HasReportedJobs));
                OnPropertyChanged(nameof(HasNoReportedJobs));
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"LoadReportsAsync failed: {ex.Message}");
            }
            finally { IsBusy = false; }
        }

        // מחיקת המשרה + כל הדיווחים עליה
        // CommandParameter="{Binding .}" ב-XAML מעביר את הפריט הנוכחי כפרמטר
        [RelayCommand]
        private async Task DeleteJobAndReport(ReportedJobSummary? summary)
        {
            if (summary == null) return;
            bool confirm = await Shell.Current.DisplayAlert("Delete Job",
                $"Delete '{summary.WorkPlaceName}' and clear all its reports? This cannot be undone.",
                "Yes, Delete", "Cancel");
            if (!confirm) return;

            IsBusy = true;
            try
            {
                await _workPlaceRepository.DeleteWorkPlaceAsync(summary.WorkPlaceId);
                await _workPlaceRepository.DeleteReportAsync(summary.WorkPlaceId);
                ReportedJobs.Remove(summary);
                OnPropertyChanged(nameof(HasReportedJobs));
                OnPropertyChanged(nameof(HasNoReportedJobs));
                await _alertService.ShowAlertAsync("Deleted", $"'{summary.WorkPlaceName}' has been removed.", "OK");
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"DeleteJobAndReport failed: {ex.Message}");
                await _alertService.ShowAlertAsync("Error", "Could not delete job. Please try again.", "OK");
            }
            finally { IsBusy = false; }
        }

        // דחיית הדיווחים בלבד — המשרה נשארת פעילה
        [RelayCommand]
        private async Task DismissReports(ReportedJobSummary? summary)
        {
            if (summary == null) return;
            bool confirm = await Shell.Current.DisplayAlert("Dismiss Reports",
                $"Mark '{summary.WorkPlaceName}' as safe and clear all reports against it?",
                "Yes, Dismiss", "Cancel");
            if (!confirm) return;

            IsBusy = true;
            try
            {
                await _workPlaceRepository.DeleteReportAsync(summary.WorkPlaceId);
                ReportedJobs.Remove(summary);
                OnPropertyChanged(nameof(HasReportedJobs));
                OnPropertyChanged(nameof(HasNoReportedJobs));
                await _alertService.ShowAlertAsync("Dismissed", "Reports cleared. The job remains active.", "OK");
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"DismissReports failed: {ex.Message}");
                await _alertService.ShowAlertAsync("Error", "Could not dismiss reports. Please try again.", "OK");
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task GoBack() => await Shell.Current.GoToAsync("..");
    }
}
