using System.Globalization;
using ODC.Web.Features;
using ODC.Web.Hosting;
using ODC.Web.Infrastructure;
using ODC.Web.Shared.Localization;
using Serilog;

// Ukrainian outside of requests too (background services, startup), whatever the server OS language is.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo(Languages.CultureOf(Languages.Ukrainian));

// Until the host builds its logger, startup failures go to the console.
Log.Logger = new LoggerConfiguration().WriteTo.Console(formatProvider: CultureInfo.InvariantCulture).CreateBootstrapLogger();

try
{
    // "migrate", "backup", "restore", "rotate-passwords": the operator's work, then stop (OperatorCommands).
    var (command, hostArguments) = OperatorCommands.Parse(args);
    var builder = WebApplication.CreateBuilder(hostArguments);
    builder.AddApplicationLogging();

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration, builder.Environment)
        .AddWebServices(builder.Configuration);

    var app = builder.Build();
    if (command is not null)
    {
        return await OperatorCommands.RunAsync(app, command);
    }

    await app.InitializeDatabaseAsync();
    app.UseWebPipeline();
    await app.RunAsync();
    return 0;
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
