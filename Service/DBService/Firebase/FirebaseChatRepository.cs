using Firebase.Database.Query;
using FirebaseWorkout.Model;

namespace FirebaseWorkout.Service.DBService.Firebase
{
    public class FirebaseChatRepository : FirebaseRealtimeService, IChatRepository
    {
        private readonly IAppLogger _appLogger;

        public FirebaseChatRepository(IAppLogger appLogger)
        {
            _appLogger = appLogger;
        }

        public async Task SendMessageAsync(string workPlaceId, string applicantId, ChatMessage message)
        {
            try
            {
                await _firebaseClient!
                    .Child("chats")
                    .Child(workPlaceId)
                    .Child(applicantId)
                    .Child("messages")
                    .PostAsync(message);

                // שמור אינדקס עבור המשתמש כדי שיוכל לראות את כל השיחות שלו
                await _firebaseClient!
                    .Child("userChats")
                    .Child(applicantId)
                    .Child(workPlaceId)
                    .PutAsync(new { WorkPlaceId = workPlaceId });
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"SendMessageAsync failed: {ex.Message}");
                throw;
            }
        }

        public async Task<List<ChatMessage>> GetMessagesAsync(string workPlaceId, string applicantId)
        {
            try
            {
                var items = await _firebaseClient!
                    .Child("chats")
                    .Child(workPlaceId)
                    .Child(applicantId)
                    .Child("messages")
                    .OnceAsync<ChatMessage>();

                return items.Select(m => new ChatMessage
                {
                    Id = m.Key,
                    SenderId = m.Object.SenderId,
                    SenderName = m.Object.SenderName,
                    Text = m.Object.Text,
                    Timestamp = m.Object.Timestamp
                })
                .OrderBy(m => m.Timestamp)
                .ToList();
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"GetMessagesAsync failed: {ex.Message}");
                return new List<ChatMessage>();
            }
        }

        public async Task<List<UserChatRoom>> GetUserChatsAsync(string userId)
        {
            try
            {
                var entries = await _firebaseClient!
                    .Child("userChats")
                    .Child(userId)
                    .OnceAsync<object>();

                var result = new List<UserChatRoom>();
                foreach (var entry in entries)
                {
                    // Get last message to find manager name
                    var msgs = await GetMessagesAsync(entry.Key, userId);
                    var managerMsg = msgs.FirstOrDefault(m => m.SenderId != userId);
                    result.Add(new UserChatRoom
                    {
                        WorkPlaceId = entry.Key,
                        WorkPlaceName = entry.Key, // will be enriched in VM
                        ManagerName = managerMsg?.SenderName ?? "Manager"
                    });
                }
                return result;
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"GetUserChatsAsync failed: {ex.Message}");
                return new List<UserChatRoom>();
            }
        }

        public async Task<List<ChatParticipant>> GetChatParticipantsAsync(string workPlaceId)
        {
            try
            {
                var applicants = await _firebaseClient!
                    .Child("chats")
                    .Child(workPlaceId)
                    .OnceAsync<object>();

                var result = new List<ChatParticipant>();
                foreach (var item in applicants)
                {
                    var msgs = await GetMessagesAsync(workPlaceId, item.Key);
                    var first = msgs.FirstOrDefault(m => m.SenderId == item.Key);
                    result.Add(new ChatParticipant
                    {
                        ApplicantId = item.Key,
                        ApplicantName = first?.SenderName ?? "User",
                        LastMessage = msgs.LastOrDefault()?.Text ?? string.Empty
                    });
                }
                return result;
            }
            catch (Exception ex)
            {
                _appLogger.LogDebug($"GetChatParticipantsAsync failed: {ex.Message}");
                return new List<ChatParticipant>();
            }
        }
    }
}
