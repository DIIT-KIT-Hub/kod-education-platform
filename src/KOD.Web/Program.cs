using KOD.Application;
using KOD.Infrastructure;
using KOD.Web.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration, builder.Environment)
    .AddWebServices(builder.Configuration);

var app = builder.Build();

await app.InitializeDatabaseAsync();
app.UseWebPipeline();
await app.RunAsync();
