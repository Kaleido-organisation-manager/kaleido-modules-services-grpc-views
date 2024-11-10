using Docker.DotNet;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using Grpc.Net.Client;
using Kaleido.Grpc.Categories;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Tests.Integrations.Fixtures.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using static Kaleido.Grpc.Categories.GrpcCategories;
using static Kaleido.Grpc.Views.GrpcViews;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Integrations.Fixtures;

public class InfrastructureFixture : IDisposable
{

    private const int TIMEOUT_WAIT_MINUTES = 1;
    private const string DB_NAME = "views";
    private const string DB_USER = "postgres";
    private const string DB_PASSWORD = "postgres";

    private string _migrationImageName = "kaleido-modules-services-grpc-views-migrations:latest";
    private string _grpcImageName = "kaleido-modules-services-grpc-views:latest";
    private string _dockerRepository = "ghcr.io";
    private string _repositoryWorkspace = "kaleido-organisation-manager";
    private string _categoryImageName = "kaleido-modules-services-grpc-categories";
    private string _categoryMigrationImageName = "kaleido-modules-services-grpc-categories-migrations";


    private readonly bool _isLocalDevelopment;
    private IFutureDockerImage? _grpcImage;
    private IFutureDockerImage? _migrationImage;
    private IContainer _migrationContainer = null!;
    private IContainer _categoryContainer = null!;
    private IContainer _categoryMigrationContainer = null!;
    private PostgreSqlContainer _postgres { get; }
    private GrpcChannel _channel { get; set; } = null!;

    public GrpcViewsClient Client { get; private set; } = null!;
    public GrpcCategoriesClient CategoriesClient { get; private set; } = null!;
    public IContainer GrpcContainer { get; private set; } = null!;
    public string ConnectionString { get; private set; } = null!;


    public InfrastructureFixture()
    {
        // Read from environment variables or appsettings.json file
        var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.Development.json", optional: true)
        .AddEnvironmentVariables()
        .Build();

        _isLocalDevelopment = configuration.GetValue<bool?>("CI") == null;

        if (!_isLocalDevelopment)
        {
            _grpcImageName = configuration.GetValue<string>("VIEWS_IMAGE_NAME") ?? _grpcImageName;
            _migrationImageName = configuration.GetValue<string>("MIGRATIONS_IMAGE_NAME") ?? _migrationImageName;
        }

        _postgres = new PostgreSqlBuilder()
            .WithDatabase(DB_NAME)
            .WithUsername(DB_USER)
            .WithPassword(DB_PASSWORD)
            .WithLogger(new LoggerFactory().CreateLogger<PostgreSqlContainer>())
            .WithPortBinding(5432, true)
            .WithExposedPort(5432)
            .WithNetworkAliases("postgres")
            .WithHostname("postgres")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilMessageIsLogged("database system is ready to accept connections"))
            .Build();

        if (_isLocalDevelopment)
        {
            var nugetUser = configuration.GetValue<string>("NUGET_USER");
            var nugetToken = configuration.GetValue<string>("NUGET_TOKEN");

            _migrationImage = new ImageFromDockerfileBuilder()
                .WithDockerfileDirectory(Path.Join(CommonDirectoryPath.GetSolutionDirectory().DirectoryPath, "../"))
                .WithDockerfile("dockerfiles/Grpc.Views.Migrations/Dockerfile.local")
                .WithName(_migrationImageName)
                .WithLogger(new LoggerFactory().CreateLogger<ImageFromDockerfileBuilder>())
                .WithBuildArgument("NUGET_USER", nugetUser)
                .WithBuildArgument("NUGET_TOKEN", nugetToken)
                .Build();

            _grpcImage = new ImageFromDockerfileBuilder()
                .WithDockerfileDirectory(Path.Join(CommonDirectoryPath.GetSolutionDirectory().DirectoryPath, "../"))
                .WithDockerfile("dockerfiles/Grpc.Views/Dockerfile.local")
                .WithName(_grpcImageName)
                .WithLogger(new LoggerFactory().CreateLogger<ImageFromDockerfileBuilder>())
                .WithBuildArgument("NUGET_USER", nugetUser)
                .WithBuildArgument("NUGET_TOKEN", nugetToken)
                .Build();
        }

        InitializeAsync().Wait();
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync().WaitAsync(TimeSpan.FromMinutes(TIMEOUT_WAIT_MINUTES));
        await _postgres.WaitForPort().WaitAsync(TimeSpan.FromMinutes(TIMEOUT_WAIT_MINUTES));

        if (_migrationImage != null)
        {
            await _migrationImage.CreateAsync().WaitAsync(TimeSpan.FromMinutes(TIMEOUT_WAIT_MINUTES));
        }

        if (_grpcImage != null)
        {
            await _grpcImage.CreateAsync().WaitAsync(TimeSpan.FromMinutes(TIMEOUT_WAIT_MINUTES));
        }


        string host = "host.testcontainers.internal";
        var postgresPort = _postgres.GetMappedPublicPort(5432);
        ConnectionString = $"Server={host};Port={postgresPort};Database={DB_NAME};Username={DB_USER};Password={DB_PASSWORD}";
        await TestcontainersSettings.ExposeHostPortsAsync(postgresPort)
            .ConfigureAwait(false);

        _categoryMigrationContainer = new ContainerBuilder()
            .WithImage($"{_dockerRepository}/{_repositoryWorkspace}/{_categoryMigrationImageName}")
            .WithEnvironment("ConnectionStrings:Categories", ConnectionString)
            .DependsOn(_postgres)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Migration completed successfully."))
            .WithLogger(new LoggerFactory().CreateLogger<IContainer>())
            .Build();

        await _categoryMigrationContainer.StartAsync().WaitAsync(TimeSpan.FromMinutes(TIMEOUT_WAIT_MINUTES));

        _categoryContainer = new ContainerBuilder()
            .WithImage($"{_dockerRepository}/{_repositoryWorkspace}/{_categoryImageName}")
            .WithEnvironment("ConnectionStrings:Categories", ConnectionString)
            .WithPortBinding(8080, true)
            .WithExposedPort(8080)
            .DependsOn(_categoryMigrationContainer)
            .WithLogger(new LoggerFactory().CreateLogger<IContainer>())
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8080))
            .Build();

        await _categoryContainer.StartAsync().WaitAsync(TimeSpan.FromMinutes(TIMEOUT_WAIT_MINUTES));

        var categoriesPort = _categoryContainer.GetMappedPublicPort(8080);
        await TestcontainersSettings.ExposeHostPortsAsync(categoriesPort);
        var categoriesUri = new UriBuilder("http", _categoryContainer.Hostname, categoriesPort);
        var categoriesChannel = GrpcChannel.ForAddress(categoriesUri.Uri.ToString());
        CategoriesClient = new GrpcCategoriesClient(categoriesChannel);

        _migrationContainer = new ContainerBuilder()
            .WithImage(_migrationImageName)
            .WithEnvironment("ConnectionStrings:Views", ConnectionString)
            .DependsOn(_categoryContainer)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Migration completed successfully."))
            .WithLogger(new LoggerFactory().CreateLogger<IContainer>())
            .Build();

        await _migrationContainer.StartAsync().WaitAsync(TimeSpan.FromMinutes(TIMEOUT_WAIT_MINUTES));

        GrpcContainer = new ContainerBuilder()
            .WithImage(_grpcImageName)
            .WithPortBinding(8080, true)
            .WithExposedPort(8080)
            .DependsOn(_migrationContainer)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8080))
            .WithEnvironment("ConnectionStrings:Views", ConnectionString)
            .WithEnvironment("ConnectionStrings:Categories", $"http://{host}:{categoriesPort}/")
            .Build();

        await GrpcContainer.StartAsync().WaitAsync(TimeSpan.FromMinutes(TIMEOUT_WAIT_MINUTES));


        var grpcPort = GrpcContainer.GetMappedPublicPort(8080);
        await TestcontainersSettings.ExposeHostPortsAsync(grpcPort);
        var grpcUri = new UriBuilder("http", GrpcContainer.Hostname, grpcPort);
        _channel = GrpcChannel.ForAddress(grpcUri.Uri.ToString());

        Client = new GrpcViewsClient(_channel);
    }


    public async Task DisposeAsync()
    {
        await _migrationContainer.DisposeAsync();
        await _postgres.DisposeAsync();
        _channel.Dispose();
        // await GrpcContainer.DisposeAsync();
        // await _categoryContainer.DisposeAsync();
        await _categoryMigrationContainer.DisposeAsync();
    }

    public void Dispose()
    {
        DisposeAsync().Wait();
    }

    public async Task ClearDatabase()
    {
        // return Task.CompletedTask;
        // TODO: Implement
        var categories = await CategoriesClient.GetAllCategoriesAsync(new Kaleido.Grpc.Categories.EmptyRequest());
        foreach (var category in categories.Categories)
        {
            if (category.Revision.Action != "Deleted")
                await CategoriesClient.DeleteCategoryAsync(new CategoryRequest { Key = category.Key });
        }

        var views = await Client.GetAllViewsAsync(new Kaleido.Grpc.Views.EmptyRequest());
        foreach (var view in views.Views)
        {
            if (view.Revision.Action != "Deleted")
                await Client.DeleteViewAsync(new ViewRequest { Key = view.Key });
        }
    }
}