using Adapters.Inbound.API.Extensions;
using Configuration;

var builder = WebApplication.CreateBuilder(args);

var configuration = new ConfigurationBuilder()

    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();


builder.Services.ConfigureAPI(configuration);

var app = builder.Build();
app.UseApiExtensions();
