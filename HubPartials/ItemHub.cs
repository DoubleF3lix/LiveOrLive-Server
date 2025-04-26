using liveorlive_server.Enums;
using Microsoft.AspNetCore.SignalR;

namespace liveorlive_server.HubPartials
{
    public partial class LiveOrLiveHub : Hub<IHubServerResponse>, IItemRequest {
        public async Task UseReverseTurnOrderItem() {

        }

        public async Task UseRackChamberItem() {
            
        }

        public async Task UseExtraLifeItem(string target) {
            
        }

        public async Task UsePickpocketItem(string target, Item item, string? itemTarget) {
            
        }

        public async Task UseLifeGambleItem() {
            
        }

        public async Task UseInvertItem() {
            
        }

        public async Task UseChamberCheckItem() {
            
        }

        public async Task UseDoubleDamageItem() {
            
        }

        public async Task UseSkipItem(string target) {
            
        }

        public async Task UseRicochetItem(string target) {

        }
    }
}