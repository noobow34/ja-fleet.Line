using jafleet.Commons.EF;
using jafleet.Line.Middleware;
using jafleet.Line.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
var config = new ConfigurationBuilder().SetBasePath(Environment.CurrentDirectory).AddJsonFile("appsettings.json").Build();

builder.Services.AddDbContextPool<JafleetContext>(
    options => options.UseNpgsql(config.GetConnectionString("DefaultConnection")).ConfigureWarnings(warnings =>
    {
        warnings.Ignore(CoreEventId.RowLimitingOperationWithoutOrderByWarning);
        warnings.Ignore(CoreEventId.FirstWithoutOrderByAndFilterWarning);
    })
);
builder.Services.AddMvc().AddNewtonsoftJson();
builder.Services.Configure<AppSettings>(config);
builder.WebHost.UseUrls("http://localhost:6500");

var app = builder.Build();

app.UseLineValidationMiddleware(config.GetSection("LineSettings")["ChannelSecret"]!);
app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}"
);


app.Run();