using System.Globalization;
using ODC.Application;
using ODC.Domain.Localization;
using ODC.Infrastructure;
using ODC.Web.Hosting;
using Serilog;

// Ukrainian outside of requests too (background services, startup), whatever the server OS language is.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo(Languages.CultureOf(Languages.Ukrainian));

// Until the host builds its logger, startup failures go to the console.
Log.Logger = new LoggerConfiguration().WriteTo.Console(formatProvider: CultureInfo.InvariantCulture).CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.AddApplicationLogging();

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration, builder.Environment)
        .AddWebServices(builder.Configuration);

    var app = builder.Build();

    await app.InitializeDatabaseAsync();
    app.UseWebPipeline();
    await app.RunAsync();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    Log.Fatal(exception, "The application failed to start");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
