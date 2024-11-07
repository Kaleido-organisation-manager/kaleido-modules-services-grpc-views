using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Kaleido.Common.Services.Grpc.Configuration.Extensions;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Configuration;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureAppConfiguration((hostingContext, config) =>
{
    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    config.AddJsonFile($"appsettings.Development.json", optional: true, reloadOnChange: true);
    config.AddEnvironmentVariables();
});

builder.ConfigureServices((hostContext, services) =>
{
    var viewsConnectionString = hostContext.Configuration.GetConnectionString("Views");
    if (string.IsNullOrEmpty(viewsConnectionString))
    {
        throw new ArgumentNullException(nameof(viewsConnectionString), "Expected a value for the views db connection string");
    }
    var assemblyName = "Kaleido.Modules.Services.Grpc.Views.Migrations";
    services.AddKaleidoMigrationEntityDbContext<ViewEntity, ViewEntityDbContext>(viewsConnectionString, assemblyName);
    services.AddKaleidoMigrationRevisionDbContext<ViewRevisionEntity, ViewRevisionDbContext>(viewsConnectionString, assemblyName);
    services.AddKaleidoMigrationEntityDbContext<CategoryViewLinkEntity, CategoryViewLinkEntityDbContext>(viewsConnectionString, assemblyName);
    services.AddKaleidoMigrationRevisionDbContext<CategoryViewLinkRevisionEntity, CategoryViewLinkRevisionEntityDbContext>(viewsConnectionString, assemblyName);

});

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var viewEntityContext = services.GetRequiredService<ViewEntityDbContext>();
    var viewRevisionContext = services.GetRequiredService<ViewRevisionDbContext>();
    var categoryViewLinkEntityContext = services.GetRequiredService<CategoryViewLinkEntityDbContext>();
    var categoryViewLinkRevisionContext = services.GetRequiredService<CategoryViewLinkRevisionEntityDbContext>();

    await viewEntityContext.Database.MigrateAsync();
    await viewRevisionContext.Database.MigrateAsync();
    await categoryViewLinkEntityContext.Database.MigrateAsync();
    await categoryViewLinkRevisionContext.Database.MigrateAsync();

    Console.WriteLine("Migration completed successfully.");
}
