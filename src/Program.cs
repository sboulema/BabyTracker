using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Quartz;
using BabyTracker.Extensions;
using BabyTracker.Jobs;
using Microsoft.AspNetCore.Builder;
using tusdotnet;
using tusdotnet.Stores;
using tusdotnet.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using BabyTracker.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.IO;
using System.Threading.Tasks;
using SendGrid.Extensions.DependencyInjection;
using tusdotnet.Models;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.Limits.MaxRequestBodySize = null;
});

builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();

    q.AddJobAndTrigger<MemoriesJob>(builder);
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddControllersWithViews();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => options.LoginPath = "/account/login")
    .AddOpenIdConnect("Auth0", options =>
    {
        options.Authority = $"https://{builder.Configuration["AUTH0_DOMAIN"]}";
        options.ClientId = builder.Configuration["AUTH0_CLIENTID"];
        options.ClientSecret = builder.Configuration["AUTH0_CLIENTSECRET"];
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
                var logoutUri = $"https://{builder.Configuration["AUTH0_DOMAIN"]}/v2/logout?client_id={builder.Configuration["AUTH0_CLIENTID"]}";

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

builder.Services.AddAuth0AuthenticationClient(config =>
{
    config.Domain = builder.Configuration["AUTH0_DOMAIN"] ?? string.Empty;
    config.ClientId = builder.Configuration["AUTH0_MACHINE_CLIENTID"];
    config.ClientSecret = builder.Configuration["AUTH0_MACHINE_CLIENTSECRET"];
});

builder.Services
    .AddAuth0ManagementClient()
    .AddManagementAccessToken();

builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ISqLiteService, SqLiteService>();
builder.Services.AddScoped<IMemoriesService, MemoriesService>();
builder.Services.AddScoped<IChartService, ChartService>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<IPictureService, PictureService>();

builder.Services.AddSendGrid(options =>
{
    options.ApiKey = builder.Configuration["SENDGRID_API_KEY"];
});

var app = builder.Build();

if (builder.Environment.IsDevelopment())
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

if (builder.Environment.IsDevelopment() &&
    !Directory.Exists($"{builder.Environment.ContentRootPath}/Data"))
{
    Directory.CreateDirectory($"{builder.Environment.ContentRootPath}/Data");
}

app.MapTus("/import", TusConfigurationFactory);

app.MapControllers();

app.Run();

Task<DefaultTusConfiguration> TusConfigurationFactory(HttpContext httpContext)
{
    var config = new DefaultTusConfiguration
    {
        Store = new TusDiskStore(builder.Environment.IsProduction() ? "/data/Data" : $"{builder.Environment.ContentRootPath}/Data"),
        Events = new()
        {
            OnFileCompleteAsync = async eventContext =>
            {
                var file = await eventContext.GetFileAsync();
                var stream = await file.GetContentAsync(eventContext.CancellationToken);

                var importService = httpContext.RequestServices.GetRequiredService<IImportService>();

                if (importService == null)
                {
                    return;
                }

                importService.Unzip(httpContext.User, stream);

                await stream.DisposeAsync();
                var terminationStore = (ITusTerminationStore)eventContext.Store;
                await terminationStore.DeleteFileAsync(file.Id, eventContext.CancellationToken);
            }
        }
    };

    return Task.FromResult(config);
}