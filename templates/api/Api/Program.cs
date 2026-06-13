using {{SolutionName}}.Api;
using {{SolutionName}}.Application;
using {{SolutionName}}.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    if (context.HostingEnvironment.IsEnvironment("Testing"))
    {
        loggerConfiguration.WriteTo.Console();
        return;
    }

    loggerConfiguration.ReadFrom.Configuration(context.Configuration).ReadFrom.Services(services);
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApi(builder.Configuration);

// EXTEND: host-level configuration that cannot live in a DependencyInjection class
// (e.g. builder.Host.UseWolverine(...)) goes here. Everything else belongs in the
// AddApplication/AddInfrastructure/AddApi extension methods.

var app = builder.Build();

// EXTEND: startup tasks (e.g. dev-time database migration) go here.

app.UseApi();

app.Run();

public partial class Program;
