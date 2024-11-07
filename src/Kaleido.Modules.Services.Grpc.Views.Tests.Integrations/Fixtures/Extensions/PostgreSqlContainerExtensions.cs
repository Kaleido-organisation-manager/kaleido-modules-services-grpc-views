using System.Net;
using System.Net.Sockets;
using DotNet.Testcontainers.Containers;
using Testcontainers.PostgreSql;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Integrations.Fixtures.Extensions;

public static class PostgreSqlContainerExtensions
{

    public static Task<bool> WaitForPort(this PostgreSqlContainer container, TimeSpan? maxWait = null)
    {
        return WaitForPort(container, PostgreSqlBuilder.PostgreSqlPort, maxWait ?? TimeSpan.FromSeconds(10));
    }

    public static async Task<bool> WaitForPort(this IContainer container, int unmappedPort, TimeSpan maxWait)
    {
        Console.WriteLine($"Waiting for port {unmappedPort} on {container.Hostname}");
        var ips = await Dns.GetHostAddressesAsync(container.Hostname);
        if (ips.Length != 1)
        {
            throw new ArgumentException($"Expected 1 IP to resolve from '{container.Hostname}', but got {ips.Length}");
        }

        int portNumber = container.GetMappedPublicPort(unmappedPort);

        CancellationTokenSource ts = new();
        ts.CancelAfter(maxWait);

        using var tcpClient = new TcpClient();

        while (!ts.IsCancellationRequested)
        {
            try
            {
                await tcpClient.ConnectAsync(ips[0], portNumber, ts.Token);
                Console.WriteLine($"Port {unmappedPort} on {container.Hostname} is open");
                return true;
            }
            catch (SocketException) { }
            await Task.Delay(500, ts.Token);
        }

        return false;
    }
}