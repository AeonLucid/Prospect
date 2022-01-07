using Prospect.Server.Api.Config;
using Prospect.Server.Api.Converters;
using Prospect.Server.Api.Middleware;
using Prospect.Server.Api.Services.Auth;
using Prospect.Server.Api.Services.Database;
using Prospect.Server.Api.Services.Qos;
using Prospect.Server.Api.Services.UserData;
using Serilog;

namespace Prospect.Server.Api;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    private IConfiguration Configuration { get; }
        
    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<AuthTokenSettings>(Configuration.GetSection(nameof(AuthTokenSettings)));
        services.Configure<DatabaseSettings>(Configuration.GetSection(nameof(DatabaseSettings)));
        services.Configure<PlayFabSettings>(Configuration.GetSection(nameof(PlayFabSettings)));

        services.AddSingleton<AuthTokenService>();
        services.AddSingleton<UserDataService>();
        services.AddSingleton<TitleDataService>();
            
        services.AddSingleton<DbUserService>();
        services.AddSingleton<DbEntityService>();
            
        services.AddSingleton<DbUserDataService>();

        services.AddHostedService<QosService>();
        services.AddSingleton<QosServer>();
        
        services.AddAuthentication(_ =>
            {
                    
            })
            .AddUserAuthentication(_ => {})
            .AddEntityAuthentication(_ => {});
            
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new DateTimeConverter());
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
            
        app.UseSerilogRequestLogging();
        app.UseMiddleware<RequestLoggerMiddleware>();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}