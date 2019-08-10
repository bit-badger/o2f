using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Uno
{
    public class App
    {
        public static void Main(string[] args)
        {
            using (var host = WebHost.CreateDefaultBuilder(args).UseStartup<Startup>().Build())
            {
                host.Run();
            }
        }
    }
}