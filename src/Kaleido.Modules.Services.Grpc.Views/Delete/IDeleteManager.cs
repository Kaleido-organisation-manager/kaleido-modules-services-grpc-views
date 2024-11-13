using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;

namespace Kaleido.Modules.Services.Grpc.Views.Delete;

public interface IDeleteManager
{
    Task<(EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>?, IEnumerable<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>)> DeleteAsync(
        Guid key,
        CancellationToken cancellationToken = default);
}