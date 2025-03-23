using liveorlive_server.Enums;
using Microsoft.AspNetCore.SignalR;

namespace liveorlive_server.HubPartials
{
    public partial class LiveOrLiveHub : Hub<IHubServerResponse>, IItemRequest {
        public async Task UseAdrenalineItem() {
            
        }

        public async Task UseChamberCheckItem() {
            
        }

        public async Task UseDoubleDamageItem() {
            
        }

        public async Task UseExtraLifeItem(string target) {
            
        }

        public async Task UseInvertItem() {
            
        }

        public async Task UsePickpocketItem(string target, Item item, string? itemTarget) {
            
        }

        public async Task UseRackChamberItem() {
            
        }

        public async Task UseReverseTurnOrderItem() {
            
        }

        public async Task UseSkipItem(string target) {
            
        }
    }
}