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
        Task JoinGameRequest(string username);
        Task SetHost(string username);
        Task KickPlayer(string username);
    }

    [Hub]
    public interface IBaseGameRequest {
        Task GameDataRequest();
        Task ShootPlayer(string target);
    }

    [Hub]
    public interface IGenericRequest {

    }

    [Hub]
    public interface IItemRequest {
        Task UseReverseTurnOrderItem();
        Task UseRackChamberItem();
        Task UseExtraLifeItem(string target);
        Task UsePickpocketItem(string target, Item item, string? itemTarget);
        Task UseAdrenalineItem();
        Task UseInvertItem();
        Task UseChamberCheckItem();
        Task UseDoubleDamageItem();
        Task UseSkipItem(string target);
    }
}
