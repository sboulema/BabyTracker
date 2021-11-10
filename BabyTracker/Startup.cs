using Auth0.AspNetCore.Authentication;
using BabyTracker.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

        services.AddSingleton<IAccountService, AccountService>();
        services.AddSingleton<ISqLiteService, SqLiteService>();
        services.AddSingleton<IMemoriesService, MemoriesService>();
        services.AddSingleton<IChartService, ChartService>();

        services.AddSendGrid(options =>
        {
            options.ApiKey = Configuration["SENDGRID_API_KEY"];
        });

        services.AddMjmlServices();

        services.Configure<CookiePolicyOptions>(options =>
        {
            options.MinimumSameSitePolicy = SameSiteMode.None;
        });

        services.AddAuth0WebAppAuthentication(options => {
            options.Domain = Configuration["AUTH0_DOMAIN"];
            options.ClientId = Configuration["AUTH0_CLIENTID"];
        });

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
