using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FirebaseWorkout.Model;
using FirebaseWorkout.Service;
using FirebaseWorkout.Service.DBService;
using System.Collections.ObjectModel;

namespace FirebaseWorkout.ViewModels
{
    // מסך צ'אט בין מועמד למנהל
    // otherUserId ריק = מועמד פותח צ'אט | מלא = מנהל צופה בצ'אט של מועמד
    [QueryProperty(nameof(WorkPlaceId), "workPlaceId")]
    [QueryProperty(nameof(WorkPlaceName), "workPlaceName")]
    [QueryProperty(nameof(OtherUserId), "otherUserId")]
    [QueryProperty(nameof(OtherUserName), "otherUserName")]
    public partial class ChatViewModel : ObservableObject
    {
        private readonly IChatRepository _chatRepository;
        private readonly IAppLogger _appLogger;

        [ObservableProperty] private string _workPlaceId = string.Empty;
        [ObservableProperty] private string _workPlaceName = string.Empty;
        [ObservableProperty] private string _otherUserId = string.Empty;
        [ObservableProperty] private string _otherUserName = string.Empty;
        [ObservableProperty] private string _messageText = string.Empty;
        [ObservableProperty] private bool _isSending;

        public ObservableCollection<ChatMessage> Messages { get; } = new();

        // Action שמגיע מה-code-behind — גורם לגלילה לתחתית הרשימה
        // (לא ניתן לגלול CollectionView ישירות מה-ViewModel)
        public Action? ScrollToBottom { get; set; }

        private string _currentUserId = string.Empty;
        private string _currentUserName = string.Empty;

        // _applicantId = המפתח לצ'אט ב-Firebase: chats/{workPlaceId}/{applicantId}/
        // אם OtherUserId ריק → המועמד הוא המשתמש הנוכחי
        // אם OtherUserId מלא → המועמד הוא OtherUserId (מנהל צופה)
        private string _applicantId = string.Empty;

        private System.Timers.Timer? _refreshTimer; // מרענן הודעות כל 3 שניות

        public ChatViewModel(IChatRepository chatRepository, IAppLogger appLogger)
        {
            _chatRepository = chatRepository;
            _appLogger = appLogger;
        }

        private void SetupChat()
        {
            if (string.IsNullOrEmpty(WorkPlaceId)) return;
            var user = (App.Current as App)?.CurrentUser;
            if (user == null) return;
            _currentUserId = user.Id;
            _currentUserName = $"{user.FirstName} {user.LastName}".Trim();
            _applicantId = string.IsNullOrEmpty(OtherUserId) ? _currentUserId : OtherUserId;
        }

        partial void OnWorkPlaceIdChanged(string value) => SetupChat();
        partial void OnOtherUserIdChanged(string value) => SetupChat();

        internal void OnAppearing()
        {
            SetupChat();
            _ = LoadMessagesAsync();
            // Timer: 3000 = 3000 מילישניות = 3 שניות
            _refreshTimer = new System.Timers.Timer(3000);
            _refreshTimer.Elapsed += (s, e) =>
                MainThread.BeginInvokeOnMainThread(async () => await LoadMessagesAsync());
            _refreshTimer.Start();
        }

        // עצירת הטיימר חובה בעזיבת המסך — אחרת ממשיך לקרוא Firebase ברקע
        internal void OnDisappearing()
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
            _refreshTimer = null;
        }

        private async Task LoadMessagesAsync()
        {
            if (string.IsNullOrEmpty(WorkPlaceId) || string.IsNullOrEmpty(_applicantId)) return;
            try
            {
                var msgs = await _chatRepository.GetMessagesAsync(WorkPlaceId, _applicantId);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (msgs.Count == Messages.Count) return; // אופטימיזציה: אל תעדכן אם אין שינוי
                    Messages.Clear();

                    string prevDate = "";
                    foreach (var m in msgs)
                    {
                        m.IsMyMessage = m.SenderId == _currentUserId; // קובע ימין/שמאל בבועת הצ'אט

                        // מחשב אם זו ההודעה הראשונה של יום חדש — אם כן, מציג כותרת תאריך
                        string msgDate = m.Timestamp.Length >= 10 ? m.Timestamp[..10] : m.Timestamp;
                        if (msgDate != prevDate)
                        {
                            m.ShowDateSeparator = true;
                            m.DateSeparatorLabel = FormatDateSeparator(msgDate);
                            prevDate = msgDate;
                        }

                        Messages.Add(m);
                    }
                    ScrollToBottom?.Invoke();
                });
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"LoadMessages failed: {ex.Message}");
            }
        }

        // ממיר תאריך (yyyy-MM-dd) לתווית קריאה: "היום" / "אתמול" / "26/05/2025"
        private static string FormatDateSeparator(string dateStr)
        {
            if (DateTime.TryParse(dateStr, out var date))
            {
                if (date.Date == DateTime.Today)           return "היום";
                if (date.Date == DateTime.Today.AddDays(-1)) return "אתמול";
                return date.ToString("dd/MM/yyyy");
            }
            return dateStr;
        }

        [RelayCommand]
        private async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(MessageText) || string.IsNullOrEmpty(_applicantId)) return;
            IsSending = true;
            try
            {
                var message = new ChatMessage
                {
                    SenderId = _currentUserId,
                    SenderName = _currentUserName,
                    Text = MessageText.Trim(),
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                await _chatRepository.SendMessageAsync(WorkPlaceId, _applicantId, message);
                MessageText = string.Empty;
                await LoadMessagesAsync();
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"SendMessage failed: {ex.Message}");
            }
            finally { IsSending = false; }
        }

        [RelayCommand]
        private async Task GoBack() => await Shell.Current.GoToAsync("..");
    }
}
