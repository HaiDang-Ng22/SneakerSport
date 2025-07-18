// Startup.cs
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(SneakerSportStore.Startup))]
namespace SneakerSportStore
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Cấu hình SignalR
            app.MapSignalR();
        }
    }
}