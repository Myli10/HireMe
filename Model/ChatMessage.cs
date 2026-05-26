namespace FirebaseWorkout.Model
{
    public class ChatMessage
    {
        public string Id { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;

        // Not stored in Firebase — computed locally
        public bool IsMyMessage { get; set; }
        public string DisplayTime => Timestamp.Length >= 16 ? Timestamp[11..16] : Timestamp;

        // מופיע מעל ההודעה הראשונה של כל יום חדש — מחושב ב-LoadMessagesAsync
        public bool ShowDateSeparator { get; set; }
        public string DateSeparatorLabel { get; set; } = string.Empty;
    }
}
