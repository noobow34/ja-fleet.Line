using jafleet.Commons.EF;
using jafleet.Line.Middleware;
using jafleet.Line.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace jafleet.Line
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {           
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContextPool<jafleetContext>(
                options => options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"))
            );
            services.AddMvc().AddNewtonsoftJson();
            services.Configure<AppSettings>(Configuration);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseLineValidationMiddleware(Configuration.GetSection("LineSettings")["ChannelSecret"]);
            app.UseStaticFiles();
            app.UseRouting();
            app.UseEndpoints(endpoint =>
            {
                endpoint.MapControllerRoute("default","{controller=Home}/{action=Index}");
            });
        }        
    }
}
