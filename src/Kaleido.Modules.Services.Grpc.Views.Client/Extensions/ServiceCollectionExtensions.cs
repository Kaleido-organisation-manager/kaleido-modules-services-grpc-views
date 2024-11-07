using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using static Kaleido.Grpc.Views.GrpcViews;

namespace Kaleido.Modules.Services.Grpc.Views.Client.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddViewsClient(this IServiceCollection services, string connectionString)
    {
        var channel = GrpcChannel.ForAddress(connectionString);
        var client = new GrpcViewsClient(channel);
        services.AddSingleton(client);
        return services;
    }
}
