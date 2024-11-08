using Kaleido.Common.Services.Grpc.Configuration.Extensions;
using Kaleido.Common.Services.Grpc.Handlers.Extensions;
using Kaleido.Common.Services.Grpc.Repositories.Extensions;
using Kaleido.Modules.Services.Grpc.Categories.Client.Extensions;
using Kaleido.Modules.Services.Grpc.Views.Common.Configuration;
using Kaleido.Modules.Services.Grpc.Views.Common.Mappers;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Services;
using Kaleido.Modules.Services.Grpc.Views.Common.Validators;
using Kaleido.Modules.Services.Grpc.Views.Create;
using Kaleido.Modules.Services.Grpc.Views.Delete;
using Kaleido.Modules.Services.Grpc.Views.Get;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

//Common
var Configuration = builder.Configuration;
var viewsConnectionString = Configuration.GetConnectionString("Views");
if (string.IsNullOrEmpty(viewsConnectionString))
{
    throw new ArgumentNullException(nameof(viewsConnectionString), "No connection string found to connect to the views database");
}

var categoriesConnectionString = Configuration.GetConnectionString("Categories");
if (string.IsNullOrEmpty(categoriesConnectionString))
{
    throw new ArgumentNullException(nameof(categoriesConnectionString), "No connection string found to connect to the categories database");
}

builder.Services.AddCategoryClient(categoriesConnectionString);

builder.Services.AddAutoMapper(typeof(ViewMappingProfile));
builder.Services.AddScoped<ViewValidator>();
builder.Services.AddScoped<KeyValidator>();
builder.Services.AddScoped<NameValidator>();

builder.Services.AddKaleidoEntityDbContext<ViewEntity, ViewEntityDbContext>(viewsConnectionString);
builder.Services.AddKaleidoRevisionDbContext<ViewRevisionEntity, ViewRevisionDbContext>(viewsConnectionString);
builder.Services.AddKaleidoEntityDbContext<CategoryViewLinkEntity, CategoryViewLinkEntityDbContext>(viewsConnectionString);
builder.Services.AddKaleidoRevisionDbContext<CategoryViewLinkRevisionEntity, CategoryViewLinkRevisionEntityDbContext>(viewsConnectionString);

builder.Services.AddEntityRepository<ViewEntity, ViewEntityDbContext>();
builder.Services.AddRevisionRepository<ViewRevisionEntity, ViewRevisionDbContext>();
builder.Services.AddEntityRepository<CategoryViewLinkEntity, CategoryViewLinkEntityDbContext>();
builder.Services.AddRevisionRepository<CategoryViewLinkRevisionEntity, CategoryViewLinkRevisionEntityDbContext>();

builder.Services.AddLifeCycleHandler<ViewEntity, ViewRevisionEntity>();
builder.Services.AddLifeCycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>();

// Create
builder.Services.AddScoped<ICreateManager, CreateManager>();
builder.Services.AddScoped<ICreateHandler, CreateHandler>();

// Delete
builder.Services.AddScoped<IDeleteManager, DeleteManager>();
builder.Services.AddScoped<IDeleteHandler, DeleteHandler>();

// Get
builder.Services.AddScoped<IGetManager, GetManager>();
builder.Services.AddScoped<IGetHandler, GetHandler>();

// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<ViewsService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
