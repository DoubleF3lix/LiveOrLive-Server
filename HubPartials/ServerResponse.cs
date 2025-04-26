using liveorlive_server.Enums;
using TypedSignalR.Client;

namespace liveorlive_server.HubPartials
{
    // These are things that are sent by the server to the client (a response to something on the server, not a response to something the receiving client did)
    // Client calls are defined in the actual partials
    [Receiver]
    public interface IHubServerResponse : IChatResponse, IGameLogResponse, IConnectionResponse, IBaseGameResponse, IGenericResponse, IItemResponse {}

    [Receiver]
    public interface IChatResponse {
        Task GetChatMessagesResponse(List<ChatMessage> messages);
        Task ChatMessageSent(ChatMessage message);
        Task ChatMessageDeleted(Guid messageId);
        Task ChatMessageEdited(Guid messageId, string content);
    }

    [Receiver]
    public interface IGameLogResponse {
        Task GetGameLogResponse(List<GameLogMessage> messages);
        Task GameLogUpdate(GameLogMessage message);
    }

    [Receiver]
    public interface IConnectionResponse {
        Task ConnectionSuccess();
        Task ConnectionFailed(string reason);
        Task PlayerJoined(Player player);
        Task PlayerLeft(string username);
        Task HostChanged(string? previous, string? current, string? reason);
        Task PlayerKicked(string username);
    }

    [Receiver]
    public interface IBaseGameResponse {
        Task GameStarted();
        Task NewRoundStarted(int blankRoundCount, int liveRoundCount);
        Task TurnStarted(string username);
        Task TurnEnded(string username);
        Task GetLobbyDataResponse(Lobby lobbyData);
        Task PlayerShotAt(string target, BulletType bulletType, int damage);
    }

    [Receiver]
    public interface IGenericResponse {
        Task ShowAlert(string message);
        Task AchievementUnlocked(string username, string achievement);
        Task ActionFailed(string reason);
    }

    [Receiver]
    public interface IItemResponse {
        Task ReverseTurnOrderItemUsed();
        Task RackChamberItemUsed();
        Task ExtraLifeItemUsed(string target);
        Task PickpocketItemUsed(string target, Item item, string? itemTarget);
        Task LifeGambleItemUsed(int lifeChange);
        Task InvertItemUsed();
        Task ChamberCheckItemUsed(BulletType bulletType);
        Task DoubleDamageItemUsed();
        Task SkipItemUsed(string target);
        Task RicochetItemUsed(string? target);
    }
}
