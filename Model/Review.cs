namespace FirebaseWorkout.Model
{
    public class Review
    {
        public string Id { get; set; } = string.Empty;
        public string WorkPlaceId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Date { get; set; } = string.Empty;

        public string RatingBadge => $"{Rating}/5";
    }
}
