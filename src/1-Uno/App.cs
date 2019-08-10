using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Uno
{
    class App
    {
        static void Main(string[] args)
        {
            using (var host = WebHost.CreateDefaultBuilder(args).UseStartup<Startup>().Build())
            {
                host.Run();
            }
        }
    }
}