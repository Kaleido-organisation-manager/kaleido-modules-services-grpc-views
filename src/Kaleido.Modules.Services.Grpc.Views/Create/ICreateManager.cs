using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;

namespace Kaleido.Modules.Services.Grpc.Views.Create;

public interface ICreateManager
{
    Task<(EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>, IEnumerable<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>)> CreateAsync(ViewEntity viewEntity, IEnumerable<string> categoryKeys, CancellationToken cancellationToken = default);
}
