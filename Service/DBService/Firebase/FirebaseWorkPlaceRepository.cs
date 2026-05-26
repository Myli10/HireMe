using Firebase.Database;
using Firebase.Database.Query;
using FirebaseWorkout.Model;

namespace FirebaseWorkout.Service.DBService.Firebase
{
    public class FirebaseWorkPlaceRepository : FirebaseRealtimeService, IWorkPlaceRepository
    {
        private readonly IAppLogger _appLogger;

        public FirebaseWorkPlaceRepository(IAppLogger appLogger)
        {
            _appLogger = appLogger;
        }

        public async Task<List<WorkPlace>> GetAllWorkPlacesAsync()
        {
            try
            {
                var items = await _firebaseClient!
                    .Child("workplaces")
                    .OnceAsync<WorkPlace>();

                return items.Select(w => new WorkPlace
                {
                    Id = w.Key,
                    Name = w.Object.Name,
                    Description = w.Object.Description,
                    Address = w.Object.Address,
                    Category = w.Object.Category,
                    WorkerRating = w.Object.WorkerRating,
                    ManagerPhone = w.Object.ManagerPhone,
                    SalaryPerHour = w.Object.SalaryPerHour,
                    ShiftHours = w.Object.ShiftHours,
                    OpeningHours = w.Object.OpeningHours,
                    CreatedByUserId = w.Object.CreatedByUserId,
                    CreatedByUserName = w.Object.CreatedByUserName,
                    Latitude = w.Object.Latitude,
                    Longitude = w.Object.Longitude,
                    IsFilled = w.Object.IsFilled,
                    ReviewCount = w.Object.ReviewCount
                }).ToList();
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"GetAllWorkPlacesAsync failed: {ex.Message}");
                return new List<WorkPlace>();
            }
        }

        public async Task AddToFavoritesAsync(string userId, string workPlaceId)
        {
            try
            {
                await _firebaseClient!
                    .Child("favorites")
                    .Child(userId)
                    .Child(workPlaceId)
                    .PutAsync(true);

                _appLogger.LogDebug($"Added {workPlaceId} to favorites for user {userId}");
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"AddToFavoritesAsync failed: {ex.Message}");
                throw new Exception("Add to favorites failed!");
            }
        }

        public async Task RemoveFromFavoritesAsync(string userId, string workPlaceId)
        {
            try
            {
                await _firebaseClient!
                    .Child("favorites")
                    .Child(userId)
                    .Child(workPlaceId)
                    .DeleteAsync();

                _appLogger.LogDebug($"Removed {workPlaceId} from favorites for user {userId}");
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"RemoveFromFavoritesAsync failed: {ex.Message}");
                throw new Exception("Remove from favorites failed!");
            }
        }

        public async Task<List<WorkPlace>> GetFavoriteJobsAsync(string userId)
        {
            try
            {
                var favoriteEntries = await _firebaseClient!
                    .Child("favorites")
                    .Child(userId)
                    .OnceAsync<bool>();

                if (!favoriteEntries.Any())
                    return new List<WorkPlace>();

                var allJobs = await GetAllWorkPlacesAsync();
                var favoriteIdSet = new HashSet<string>(favoriteEntries.Select(f => f.Key));
                return allJobs.Where(j => favoriteIdSet.Contains(j.Id)).ToList();
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"GetFavoriteJobsAsync failed: {ex.Message}");
                return new List<WorkPlace>();
            }
        }

        public async Task<bool> IsFavoriteAsync(string userId, string workPlaceId)
        {
            try
            {
                var favorites = await _firebaseClient!
                    .Child("favorites")
                    .Child(userId)
                    .OnceAsync<bool>();

                return favorites.Any(f => f.Key == workPlaceId);
            }
            catch
            {
                return false;
            }
        }

        public async Task AddWorkPlaceAsync(WorkPlace workPlace)
        {
            try
            {
                var result = await _firebaseClient!
                    .Child("workplaces")
                    .PostAsync(workPlace);

                _appLogger.LogDebug($"WorkPlace '{workPlace.Name}' added with key {result.Key}");
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"AddWorkPlaceAsync failed: {ex.Message}");
                throw new Exception("Failed to add workplace!");
            }
        }

        public async Task EditWorkPlaceAsync(WorkPlace workPlace)
        {
            try
            {
                await _firebaseClient!
                    .Child("workplaces")
                    .Child(workPlace.Id)
                    .PutAsync(workPlace);
                _appLogger.LogDebug($"WorkPlace '{workPlace.Name}' updated.");
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"EditWorkPlaceAsync failed: {ex.Message}");
                throw new Exception("Failed to update workplace!");
            }
        }

        public async Task DeleteWorkPlaceAsync(string workPlaceId)
        {
            try
            {
                await _firebaseClient!.Child("workplaces").Child(workPlaceId).DeleteAsync();
                _appLogger.LogDebug($"WorkPlace {workPlaceId} deleted.");
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"DeleteWorkPlaceAsync failed: {ex.Message}");
                throw new Exception("Failed to delete workplace!");
            }
        }

        public async Task<List<Review>> GetReviewsAsync(string workPlaceId)
        {
            try
            {
                var items = await _firebaseClient!
                    .Child("workplaceReviews")
                    .Child(workPlaceId)
                    .OnceAsync<Review>();

                return items.Select(r => new Review
                {
                    Id = r.Key,
                    WorkPlaceId = workPlaceId,
                    UserId = r.Object.UserId,
                    UserName = r.Object.UserName,
                    Text = r.Object.Text,
                    Rating = r.Object.Rating,
                    Date = r.Object.Date
                }).ToList();
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"GetReviewsAsync failed: {ex.Message}");
                return new List<Review>();
            }
        }

        public async Task AddReviewAsync(Review review)
        {
            try
            {
                await _firebaseClient!
                    .Child("workplaceReviews")
                    .Child(review.WorkPlaceId)
                    .PostAsync(review);

                // Update average rating in the workplace node
                var reviews = await GetReviewsAsync(review.WorkPlaceId);
                double avg = reviews.Any() ? reviews.Average(r => r.Rating) : review.Rating;
                await _firebaseClient!
                    .Child("workplaces")
                    .Child(review.WorkPlaceId)
                    .PatchAsync(new { WorkerRating = Math.Round(avg, 1), ReviewCount = reviews.Count });

                _appLogger.LogDebug($"Review added for workPlace {review.WorkPlaceId}");
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"AddReviewAsync failed: {ex.Message}");
                throw new Exception("Failed to add review!");
            }
        }

        public async Task ReportWorkPlaceAsync(WorkPlaceReport report)
        {
            try
            {
                await _firebaseClient!
                    .Child("reports")
                    .Child(report.WorkPlaceId)
                    .Child(report.ReportedByUserId)
                    .PutAsync(report);

                _appLogger.LogDebug($"WorkPlace {report.WorkPlaceId} reported by {report.ReportedByUserId}");
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"ReportWorkPlaceAsync failed: {ex.Message}");
                throw new Exception("Failed to submit report!");
            }
        }

        public async Task<bool> HasUserReportedAsync(string workPlaceId, string userId)
        {
            try
            {
                var entry = await _firebaseClient!
                    .Child("reports")
                    .Child(workPlaceId)
                    .Child(userId)
                    .OnceSingleAsync<WorkPlaceReport>();
                return entry != null;
            }
            catch { return false; }
        }

        public async Task<List<ReportedJobSummary>> GetAllReportedJobsAsync()
        {
            try
            {
                var allWorkPlaces = await GetAllWorkPlacesAsync();
                var workPlaceMap = allWorkPlaces.ToDictionary(w => w.Id);

                // Each child of "reports" is a workPlaceId node containing userId → WorkPlaceReport entries
                var workPlaceNodes = await _firebaseClient!
                    .Child("reports")
                    .OnceAsync<object>();

                var summaries = new List<ReportedJobSummary>();
                foreach (var node in workPlaceNodes)
                {
                    var workPlaceId = node.Key;
                    var reportEntries = await _firebaseClient!
                        .Child("reports")
                        .Child(workPlaceId)
                        .OnceAsync<WorkPlaceReport>();

                    var reports = reportEntries.Select(r => r.Object).ToList();
                    workPlaceMap.TryGetValue(workPlaceId, out var wp);

                    summaries.Add(new ReportedJobSummary
                    {
                        WorkPlaceId = workPlaceId,
                        WorkPlaceName = wp?.Name ?? "Unknown",
                        CreatedByUserName = wp?.CreatedByUserName ?? "Unknown",
                        ReportCount = reports.Count,
                        Reports = reports
                    });
                }

                return summaries.OrderByDescending(s => s.ReportCount).ToList();
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"GetAllReportedJobsAsync failed: {ex.Message}");
                return new List<ReportedJobSummary>();
            }
        }

        public async Task DeleteReportAsync(string workPlaceId)
        {
            try
            {
                await _firebaseClient!.Child("reports").Child(workPlaceId).DeleteAsync();
                _appLogger.LogDebug($"Report node for {workPlaceId} deleted.");
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"DeleteReportAsync failed: {ex.Message}");
                throw new Exception("Failed to delete report!");
            }
        }

        public async Task SetWorkPlaceFilledAsync(string workPlaceId, bool isFilled)
        {
            try
            {
                await _firebaseClient!
                    .Child("workplaces")
                    .Child(workPlaceId)
                    .PatchAsync(new { IsFilled = isFilled });
                _appLogger.LogDebug($"WorkPlace {workPlaceId} IsFilled set to {isFilled}.");
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"SetWorkPlaceFilledAsync failed: {ex.Message}");
                throw new Exception("Failed to update filled status!");
            }
        }
    }
}
