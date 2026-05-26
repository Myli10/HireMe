using FirebaseWorkout.Model;
using FirebaseWorkout.ViewModels;

namespace FirebaseWorkout.Views;

public partial class ReportedJobsView : ContentPage
{
    public ReportedJobsView(ReportedJobsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as ReportedJobsViewModel)?.OnAppearing();
    }

    private void OnDismissClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is ReportedJobSummary summary)
            (BindingContext as ReportedJobsViewModel)?.DismissReportsCommand.Execute(summary);
    }

    private void OnDeleteJobClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is ReportedJobSummary summary)
            (BindingContext as ReportedJobsViewModel)?.DeleteJobAndReportCommand.Execute(summary);
    }
}
