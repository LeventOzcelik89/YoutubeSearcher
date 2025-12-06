using Microsoft.AspNetCore.SignalR;

namespace YoutubeSearcher.Web.Hubs
{
    public class SearchHub : Hub
    {
        public async Task JoinSearchGroup(string searchId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, searchId);
        }

        public async Task LeaveSearchGroup(string searchId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, searchId);
        }

        public async Task JoinPlaylistSearchGroup(string searchId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, searchId);
        }

        public async Task LeavePlaylistSearchGroup(string searchId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, searchId);
        }
    }
}

