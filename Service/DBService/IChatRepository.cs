using FirebaseWorkout.Model;

namespace FirebaseWorkout.Service.DBService
{
    public interface IChatRepository
    {
        Task SendMessageAsync(string workPlaceId, string applicantId, ChatMessage message);
        Task<List<ChatMessage>> GetMessagesAsync(string workPlaceId, string applicantId);
        Task<List<ChatParticipant>> GetChatParticipantsAsync(string workPlaceId);
        Task<List<UserChatRoom>> GetUserChatsAsync(string userId);
    }
}
