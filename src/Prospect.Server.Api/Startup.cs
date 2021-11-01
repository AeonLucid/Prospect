using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prospect.Server.Api.Config;
using Prospect.Server.Api.Middleware;
using Prospect.Server.Api.Services.Auth;
using Prospect.Server.Api.Services.Auth.User;
using Prospect.Server.Api.Services.Database;
using Serilog;

namespace Prospect.Server.Api
{
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
            services.AddSingleton<DbUserService>();
            services.AddSingleton<DbEntityService>();

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = UserAuthenticationOptions.DefaultScheme;
                    options.DefaultChallengeScheme = UserAuthenticationOptions.DefaultScheme;
                })
                .AddUserAuthentication(_ => {})
                .AddEntityAuthentication(_ => {});
            
            services.AddControllers();
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
}