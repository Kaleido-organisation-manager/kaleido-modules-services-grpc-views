using Kaleido.Common.Services.Grpc.Constants;
using Kaleido.Common.Services.Grpc.Handlers.Interfaces;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;

namespace Kaleido.Modules.Services.Grpc.Views.GetAllRevisions;

public class GetAllRevisionsManager : IGetAllRevisionsManager
{
    private readonly IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity> _viewLifecycleHandler;
    private readonly IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity> _categoryViewLinkLifecycleHandler;

    public GetAllRevisionsManager(
        IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity> viewLifecycleHandler,
        IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity> categoryViewLinkLifecycleHandler
        )
    {
        _viewLifecycleHandler = viewLifecycleHandler;
        _categoryViewLinkLifecycleHandler = categoryViewLinkLifecycleHandler;
    }

    public async Task<IEnumerable<(EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>, IEnumerable<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>)>> GetAllRevisionAsync(Guid key, CancellationToken cancellationToken)
    {
        var viewEntityRevisions = await _viewLifecycleHandler.GetAllAsync(key, cancellationToken: cancellationToken);
        var categoryViewLinks = await _categoryViewLinkLifecycleHandler.FindAllAsync(x => x.ViewKey == key, cancellationToken: cancellationToken);

        var historicTimeSlices = viewEntityRevisions.Select(x => x.Revision.CreatedAt).Distinct().ToList();
        historicTimeSlices.AddRange(categoryViewLinks.Select(x => x.Revision.CreatedAt).Distinct());
        historicTimeSlices = [.. historicTimeSlices.Distinct().OrderByDescending(x => x)];

        var results = new List<(EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>, IEnumerable<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>)>();

        foreach (var timeSlice in historicTimeSlices)
        {
            var viewEntityRevision = viewEntityRevisions.FirstOrDefault(x => x.Revision.CreatedAt <= timeSlice);
            var categoryViewLinksRevisions = categoryViewLinks.Where(x => x.Revision.CreatedAt <= timeSlice).OrderByDescending(x => x.Revision.Revision).DistinctBy(x => x.Key).ToList();

            if (viewEntityRevision != null)
            {
                results.Add((viewEntityRevision, categoryViewLinksRevisions));
            }
        }

        return results;
    }
}
