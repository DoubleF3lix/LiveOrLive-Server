using LiveOrLiveServer.Enums;
using LiveOrLiveServer.Models;
using LiveOrLiveServer.Models.Results;
using TypedSignalR.Client;

namespace LiveOrLiveServer.HubPartials
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
        Task ClientJoined(ConnectedClient client);
        Task ClientLeft(string username);
        Task HostChanged(string? previous, string? current, string? reason);
        Task ClientKicked(string username);
        Task ClientTypeChanged(ConnectedClient newClient);
    }

    [Receiver]
    public interface IBaseGameResponse {
        Task GameStarted(List<string> turnOrder);
        Task GameEnded(string? winner, List<string> purgedPlayers);
        Task NewRoundStarted(NewRoundResult result);
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
        Task ReverseTurnOrderItemUsed(string itemSourceUsername);
        Task RackChamberItemUsed(BulletType bulletType, string itemSourceUsername);
        Task ExtraLifeItemUsed(string target, string itemSourceUsername);
        // The itemSourceUsername is always currentTurn, but for consistency we pass it anyway
        Task PickpocketItemUsed(string target, Item item, string? itemTarget, string itemSourceUsername);
        Task LifeGambleItemUsed(int lifeChange, string itemSourceUsername);
        Task InvertItemUsed(string itemSourceUsername);
        // bulletType can be null if we want to hide the bullet type from others
        Task ChamberCheckItemUsed(BulletType? bulletType, string itemSourceUsername);
        Task DoubleDamageItemUsed(string itemSourceUsername);
        Task SkipItemUsed(string target, string itemSourceUsername);
        Task RicochetItemUsed(string? target, string itemSourceUsername);
    }
}
