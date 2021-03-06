using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Nancy.Owin;

namespace Dos
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app) =>
            app.UseOwin(x => x.UseNancy(options => options.Bootstrapper = new DosBootstrapper()));
    }
}