using Kaleido.Common.Services.Grpc.Constants;
using Kaleido.Common.Services.Grpc.Handlers.Interfaces;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;

namespace Kaleido.Modules.Services.Grpc.Views.Get;

public class GetManager : IGetManager
{
    private readonly IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity> _viewLifecycleHandler;
    private readonly IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity> _categoryViewLinkLifecycleHandler;

    public GetManager(IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity> viewLifecycleHandler, IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity> categoryViewLinkLifecycleHandler)
    {
        _viewLifecycleHandler = viewLifecycleHandler;
        _categoryViewLinkLifecycleHandler = categoryViewLinkLifecycleHandler;
    }

    public async Task<(EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>?, IEnumerable<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>)> GetAsync(Guid key, CancellationToken cancellationToken)
    {
        var viewResult = await _viewLifecycleHandler.GetAsync(key, cancellationToken: cancellationToken);

        if (viewResult is null || viewResult.Revision?.Action == RevisionAction.Deleted)
        {
            return (null, Enumerable.Empty<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>());
        }

        var categoryViewLinkResults = await _categoryViewLinkLifecycleHandler.FindAllAsync(link => link.ViewKey == key, cancellationToken: cancellationToken);
        var latestCategoryViewLinkResults = categoryViewLinkResults.GroupBy(x => x.Key).Select(x => x.OrderByDescending(y => y.Revision.Revision).First());
        categoryViewLinkResults = latestCategoryViewLinkResults.Where(r => r.Revision.Action != RevisionAction.Deleted);

        return (viewResult, categoryViewLinkResults);
    }
}