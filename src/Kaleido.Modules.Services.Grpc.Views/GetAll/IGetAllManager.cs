using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;

namespace Kaleido.Modules.Services.Grpc.Views.GetAll;

public interface IGetAllManager
{

    Task<IEnumerable<(EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>, IEnumerable<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>)>> GetAllAsync(CancellationToken cancellationToken);
}