using Microsoft.AspNetCore.Hosting;

namespace Uno
{
    class App
    {
        static void Main(string[] args)
        {
            using (var host = new WebHostBuilder().UseKestrel().UseStartup<Startup>().Build())
            {
                host.Run();
            }
        }
    }
}