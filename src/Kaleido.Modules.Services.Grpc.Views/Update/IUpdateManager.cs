using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;

namespace Kaleido.Modules.Services.Grpc.Views.Update;

public interface IUpdateManager
{
    Task<(EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>, IEnumerable<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>)> UpdateAsync(Guid key, ViewEntity viewEntity, IEnumerable<string> categoryKeys, CancellationToken cancellationToken = default);
}