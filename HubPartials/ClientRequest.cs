using liveorlive_server.Enums;
using TypedSignalR.Client;

namespace liveorlive_server.HubPartials
{
    [Hub]
    public interface IChatRequest {
        Task GetChatMessagesRequest();
        Task SendChatMessage(string content);
        Task DeleteChatMessage(Guid messageId);
        Task EditChatMessage(Guid messageId, string content);
    }

    [Hub]
    public interface IGameLogRequest {
        Task GetGameLogRequest();
    }

    [Hub]
    public interface IConnectionRequest {
        Task SetHost(string username);
        Task KickPlayer(string username);
        Task ChangeClientType(ClientType clientType);
    }

    [Hub]
    public interface IBaseGameRequest {
        Task StartGame();
        Task GetLobbyDataRequest();
        Task ShootPlayer(string target);
    }

    [Hub]
    public interface IGenericRequest {}

    [Hub]
    public interface IItemRequest {
        Task UseReverseTurnOrderItem();
        Task UseRackChamberItem();
        Task UseExtraLifeItem(string target);
        Task UsePickpocketItem(string target, Item item, string? itemTarget);
        Task UseLifeGambleItem();
        Task UseInvertItem();
        Task UseChamberCheckItem();
        Task UseDoubleDamageItem();
        Task UseSkipItem(string target);
        Task UseRicochetItem(string target);
    }
}
