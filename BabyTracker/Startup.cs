using System;
using System.Threading.Tasks;
using Auth0.AuthenticationApi;
using BabyTracker.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Mjml.AspNetCore;
using SendGrid.Extensions.DependencyInjection;

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

        services.AddControllersWithViews();

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

        services.AddSendGrid(options =>
        {
            options.ApiKey = Configuration["SENDGRID_API_KEY"];
        });

        services.AddMjmlServices();

        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = int.MaxValue;
            options.MemoryBufferThreshold = int.MaxValue;
            options.ValueCountLimit = int.MaxValue;
            options.ValueLengthLimit = int.MaxValue;
            options.MultipartBodyLengthLimit = long.MaxValue;
        });
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
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
        });
    }
}
