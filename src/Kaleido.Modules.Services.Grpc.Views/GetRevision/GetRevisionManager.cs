using Kaleido.Common.Services.Grpc.Handlers.Interfaces;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;

namespace Kaleido.Modules.Services.Grpc.Views.GetRevision;

public class GetRevisionManager : IGetRevisionManager
{

    private readonly IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity> _viewLifecycleHandler;
    private readonly IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity> _categoryViewLinkLifecycleHandler;

    public GetRevisionManager(IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity> viewLifecycleHandler, IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity> categoryViewLinkLifecycleHandler)
    {
        _viewLifecycleHandler = viewLifecycleHandler;
        _categoryViewLinkLifecycleHandler = categoryViewLinkLifecycleHandler;
    }

    public async Task<(EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>?, IEnumerable<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>)> GetViewRevision(Guid key, DateTime createdAt, CancellationToken cancellationToken)
    {
        var viewResult = await _viewLifecycleHandler.GetHistoricAsync(key, createdAt, cancellationToken);

        if (viewResult is null)
        {
            return (null, Enumerable.Empty<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>());
        }

        var categoryViewLinkResults = await _categoryViewLinkLifecycleHandler.FindAllAsync(link => link.ViewKey == key, r => r.CreatedAt == createdAt, cancellationToken: cancellationToken);

        return (viewResult, categoryViewLinkResults);
    }
}
