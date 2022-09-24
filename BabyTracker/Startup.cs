using System;
using System.IO;
using System.Threading.Tasks;
using Auth0.AuthenticationApi;
using BabyTracker.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using SendGrid.Extensions.DependencyInjection;
using tusdotnet;
using tusdotnet.Interfaces;
using tusdotnet.Models;
using tusdotnet.Models.Configuration;
using tusdotnet.Stores;

namespace BabyTracker;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting(options => options.LowercaseUrls = true);

        services
            .AddControllersWithViews();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options => options.LoginPath = "/account/login")
            .AddOpenIdConnect("Auth0", options =>
            {
                options.Authority = $"https://{Configuration["AUTH0_DOMAIN"]}";
                options.ClientId = Configuration["AUTH0_CLIENTID"];
                options.ClientSecret = Configuration["AUTH0_CLIENTSECRET"];
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.CallbackPath = new PathString("/callback");
                options.ClaimsIssuer = "Auth0";
                options.Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProviderForSignOut = (context) =>
                    {
                        var logoutUri = $"https://{Configuration["AUTH0_DOMAIN"]}/v2/logout?client_id={Configuration["AUTH0_CLIENTID"]}";

                        var postLogoutUri = context.Properties.RedirectUri;
                        if (!string.IsNullOrEmpty(postLogoutUri))
                        {
                            if (postLogoutUri.StartsWith("/"))
                            {
                                // transform to absolute
                                var request = context.Request;
                                postLogoutUri = request.Scheme + "://" + request.Host + request.PathBase + postLogoutUri;
                            }

                            logoutUri += $"&returnTo={Uri.EscapeDataString(postLogoutUri)}";
                        }

                        context.Response.Redirect(logoutUri);
                        context.HandleResponse();

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddSingleton(x =>
            new AuthenticationApiClient(new Uri($"https://{Configuration["AUTH0_DOMAIN"]}/")));

        services.AddSingleton<IAccountService, AccountService>();
        services.AddSingleton<ISqLiteService, SqLiteService>();
        services.AddSingleton<IMemoriesService, MemoriesService>();
        services.AddSingleton<IChartService, ChartService>();
        services.AddSingleton<IImportService, ImportService>();
        services.AddSingleton<IPictureService, PictureService>();

        services.AddSendGrid(options =>
        {
            options.ApiKey = Configuration["SENDGRID_API_KEY"];
        });

        services.AddHttpContextAccessor();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();

        app.UseAuthorization();

        if (env.IsDevelopment() &&
            !Directory.Exists($"{env.ContentRootPath}/Data"))
        {
            Directory.CreateDirectory($"{env.ContentRootPath}/Data");
        }

        app.UseTus(httpContext => new DefaultTusConfiguration
        {
            Store = new TusDiskStore(env.IsProduction() ? "/data/Data" : $"{env.ContentRootPath}/Data"),
            UrlPath = "/import",
            Events = new Events
            {
                OnFileCompleteAsync = async eventContext =>
                {
                    var file = await eventContext.GetFileAsync();
                    var stream = await file.GetContentAsync(eventContext.CancellationToken);

                    var importService = app.ApplicationServices.GetService<IImportService>();

                    await importService?.Unzip(stream);

                    await stream.DisposeAsync();
                    var terminationStore = (ITusTerminationStore)eventContext.Store;
                    await terminationStore.DeleteFileAsync(file.Id, eventContext.CancellationToken);
                }
            }
        });

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
        });
    }
}
