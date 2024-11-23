using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;

namespace Kaleido.Modules.Services.Grpc.Views.GetRevision;

public interface IGetRevisionManager
{
    Task<ManagerResponse> GetViewRevision(Guid key, DateTime createdAt, CancellationToken cancellationToken);
}
