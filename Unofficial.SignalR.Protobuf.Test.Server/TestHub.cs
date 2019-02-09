using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Unofficial.SignalR.Protobuf.Test.Server
{
    public class TestHub : Hub
    {
        public override Task OnConnectedAsync()
        {


            return base.OnConnectedAsync();
        }
    }
}
