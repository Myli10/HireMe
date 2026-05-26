using FirebaseWorkout.Model;

namespace FirebaseWorkout.Service.DBService
{
    public interface IWorkPlaceRepository
    {
        Task<List<WorkPlace>> GetAllWorkPlacesAsync();
        Task AddToFavoritesAsync(string userId, string workPlaceId);
        Task RemoveFromFavoritesAsync(string userId, string workPlaceId);
        Task<List<WorkPlace>> GetFavoriteJobsAsync(string userId);
        Task<bool> IsFavoriteAsync(string userId, string workPlaceId);
        Task AddWorkPlaceAsync(WorkPlace workPlace);
        Task EditWorkPlaceAsync(WorkPlace workPlace);
        Task DeleteWorkPlaceAsync(string workPlaceId);
        Task<List<Review>> GetReviewsAsync(string workPlaceId);
        Task AddReviewAsync(Review review);
        Task ReportWorkPlaceAsync(WorkPlaceReport report);
        Task<bool> HasUserReportedAsync(string workPlaceId, string userId);
        Task<List<ReportedJobSummary>> GetAllReportedJobsAsync();
        Task DeleteReportAsync(string workPlaceId);
        Task SetWorkPlaceFilledAsync(string workPlaceId, bool isFilled);
    }
}
