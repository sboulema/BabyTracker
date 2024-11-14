using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Quartz;
using BabyTracker.Jobs;
using Microsoft.AspNetCore.Builder;
using tusdotnet;
using tusdotnet.Stores;
using tusdotnet.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using BabyTracker.Services;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using SendGrid.Extensions.DependencyInjection;
using tusdotnet.Models;
using Auth0Net.DependencyInjection;
using Quartz.AspNetCore;
using Microsoft.Data.Sqlite;
using BabyTracker.Policies;

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

builder.Services.AddQuartzServer(q => q.WaitForJobsToComplete = true);

builder.Services.AddRouting(options => options.LowercaseUrls = true);

builder.Services.AddOutputCache(options => options.AddPolicy("AuthenticatedOutputCache", new AuthenticatedOutputCachePolicy()));

builder.Services.AddControllersWithViews();

builder.Services
	.AddAuthentication()
	.AddCookie(options => options.LoginPath = "/account/login");

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

app.UseRouting();

app.UseOutputCache();

app.UseAuthentication();

app.UseAuthorization();

if (builder.Environment.IsDevelopment() &&
	!Directory.Exists($"{builder.Environment.ContentRootPath}/Data"))
{
	Directory.CreateDirectory($"{builder.Environment.ContentRootPath}/Data");
}

app.MapStaticAssets();

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