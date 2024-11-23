using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;

namespace Kaleido.Modules.Services.Grpc.Views.Delete;

public interface IDeleteManager
{
    Task<ManagerResponse> DeleteAsync(Guid key, CancellationToken cancellationToken = default);
}