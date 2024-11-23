using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;

namespace Kaleido.Modules.Services.Grpc.Views.Get;

public interface IGetManager
{
    Task<ManagerResponse> GetAsync(Guid key, CancellationToken cancellationToken);
}
