using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;

namespace Kaleido.Modules.Services.Grpc.Views.Create;

public interface ICreateManager
{
    Task<ManagerResponse> CreateAsync(ViewEntity viewEntity, IEnumerable<string> categoryKeys, CancellationToken cancellationToken = default);
}
