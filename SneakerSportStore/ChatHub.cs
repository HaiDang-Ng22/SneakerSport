using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;

namespace SneakerSportStore.Hubs
{
    public class ChatHub : Hub
    {
        public async Task JoinConversation(string conversationId)
        {
            await Groups.Add(Context.ConnectionId, conversationId);
        }
    }
}