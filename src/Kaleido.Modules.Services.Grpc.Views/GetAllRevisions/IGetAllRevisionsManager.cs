using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;

namespace Kaleido.Modules.Services.Grpc.Views.GetAllRevisions;

public interface IGetAllRevisionsManager
{
    Task<IEnumerable<ManagerResponse>> GetAllRevisionAsync(Guid key, CancellationToken cancellationToken);
}
