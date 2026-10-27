using System;
using System.IO;
using System.Threading.Tasks;
using Auth0Net.DependencyInjection;
using BabyTracker.Jobs;
using BabyTracker.Policies;
using BabyTracker.Repositories;
using BabyTracker.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using tusdotnet;
using tusdotnet.Interfaces;
using tusdotnet.Models;
using tusdotnet.Stores;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(kestrel =>
{
	kestrel.Limits.MaxRequestBodySize = null;
});

builder.Services.AddQuartz(q =>
{
	q.ScheduleJob<MemoriesJob>(trigger => trigger
		.WithIdentity("MemoriesJob Trigger")
		.StartNow()
		.WithCronSchedule(builder.Configuration["MEMORIES_CRON"] ?? "0 0 6 ? * * *")
	);
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

builder.Services.AddRouting(options => options.LowercaseUrls = true);

builder.Services.AddOutputCache(options => options.AddPolicy("AuthenticatedOutputCache", new AuthenticatedOutputCachePolicy()));

builder.Services.AddControllersWithViews();

builder.Services
	.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie(options =>
	{
		options.LoginPath = "/account/login";
		options.Cookie.Name = "BabyTracker.Auth";
		options.Cookie.HttpOnly = true;
		options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
		options.Cookie.SameSite = SameSiteMode.Lax;
		options.ExpireTimeSpan = TimeSpan.FromDays(30);
		options.SlidingExpiration = true;
	});

builder.Services.AddAuth0AuthenticationClient(config =>
{
	config.Domain = builder.Configuration["AUTH0_DOMAIN"] ?? string.Empty;
	config.ClientId = builder.Configuration["AUTH0_MACHINE_CLIENTID"];
	config.ClientSecret = builder.Configuration["AUTH0_MACHINE_CLIENTSECRET"];
});

builder.Services
	.AddAuth0ManagementClient();

builder.Services
	.AddScoped<IAccountService, AccountService>()
	.AddScoped<ISqLiteService, SqLiteService>()
	.AddScoped<IMemoriesService, MemoriesService>()
	.AddScoped<IChartService, ChartService>()
	.AddScoped<IImportService, ImportService>()
	.AddScoped<IPictureService, PictureService>()
	.AddScoped<IEmailRepository, EmailRepository>();

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

app.UseOutputCache();

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

				// Clear Sqlite connection pool, so the database file is not locked
				// and can be overwritten with a newer version
				SqliteConnection.ClearAllPools();

				await importService.Unzip(httpContext.User, stream);

				await stream.DisposeAsync();
				var terminationStore = (ITusTerminationStore)eventContext.Store;
				await terminationStore.DeleteFileAsync(file.Id, eventContext.CancellationToken);
			}
		}
	};

	return Task.FromResult(config);
}