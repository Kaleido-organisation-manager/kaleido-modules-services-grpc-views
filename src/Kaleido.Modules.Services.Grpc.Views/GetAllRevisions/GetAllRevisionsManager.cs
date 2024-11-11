using AutoMapper;
using Kaleido.Common.Services.Grpc.Constants;
using Kaleido.Common.Services.Grpc.Handlers.Interfaces;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;

namespace Kaleido.Modules.Services.Grpc.Views.GetAllRevisions;

public class GetAllRevisionsManager : IGetAllRevisionsManager
{
    private readonly IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity> _viewLifecycleHandler;
    private readonly IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity> _categoryViewLinkLifecycleHandler;
    private readonly IMapper _mapper;

    public GetAllRevisionsManager(
        IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity> viewLifecycleHandler,
        IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity> categoryViewLinkLifecycleHandler,
        IMapper mapper
        )
    {
        _viewLifecycleHandler = viewLifecycleHandler;
        _categoryViewLinkLifecycleHandler = categoryViewLinkLifecycleHandler;
        _mapper = mapper;
    }

    public async Task<IEnumerable<(EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>, IEnumerable<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>)>> GetAllRevisionAsync(Guid key, CancellationToken cancellationToken)
    {
        var viewEntityRevisions = await _viewLifecycleHandler.GetAllAsync(key, cancellationToken: cancellationToken);
        var categoryViewLinks = await _categoryViewLinkLifecycleHandler.FindAllAsync(x => x.ViewKey == key, cancellationToken: cancellationToken);

        // Group all changes by timestamp with a small tolerance for slight differences
        var historicTimeSlices = viewEntityRevisions
            .Select(x => x.Revision.CreatedAt)
            .Concat(categoryViewLinks.Select(x => x.Revision.CreatedAt))
            .Distinct()
            .OrderByDescending(x => x)
            .ToList();


        var compositeRevisions = new List<(EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>, IEnumerable<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>)>();

        foreach (var timeSlice in historicTimeSlices)
        {
            var viewEntityRevision = viewEntityRevisions.Where(x => x.Revision.CreatedAt <= timeSlice)
                .GroupBy(x => x.Key)
                .Select(x => x.OrderByDescending(y => y.Revision.Revision).First())
                .Select(x => _mapper.Map<EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>>(x))
                .FirstOrDefault();
            var categoryViewLinksRevisions = categoryViewLinks
                .Where(x => x.Revision.CreatedAt <= timeSlice)
                .OrderByDescending(x => x.Revision.Revision)
                .GroupBy(x => x.Key)
                .Select(x => x.OrderByDescending(y => y.Revision.Revision).First())
                .Select(x => _mapper.Map<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>(x))
                .ToList();

            if (viewEntityRevision != null)
            {
                compositeRevisions.Add((viewEntityRevision, categoryViewLinksRevisions));
            }
        }

        var results = new List<(EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>, IEnumerable<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>)>();

        for (int i = 0; i < compositeRevisions.Count; i++)
        {
            var (viewEntityRevision, categoryViewLinksRevisions) = compositeRevisions[i];

            EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>? previousViewEntityRevision = null;
            IEnumerable<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>? previousCategoryViewLinksRevisions = null;

            if (i < compositeRevisions.Count - 1)
            {
                (previousViewEntityRevision, previousCategoryViewLinksRevisions) = compositeRevisions[i + 1];
            }

            if (previousViewEntityRevision != null && previousViewEntityRevision.Revision.Revision == viewEntityRevision.Revision.Revision)
            {
                viewEntityRevision = _mapper.Map<EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>>(viewEntityRevision);
                viewEntityRevision.Revision.Action = RevisionAction.Unmodified;
            }

            var previousDeletedCategories = previousCategoryViewLinksRevisions?.Where(x => x.Revision.Action == RevisionAction.Deleted).ToList() ?? new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>();
            var resultingCategories = categoryViewLinksRevisions
                .Where(x => !previousDeletedCategories.Any(y => y.Key == x.Key))
                .Select(x =>
                {
                    if (previousCategoryViewLinksRevisions != null && previousCategoryViewLinksRevisions.Any(y => y.Key == x.Key && y.Revision.Action == x.Revision.Action))
                    {
                        x.Revision.Action = RevisionAction.Unmodified;
                    }
                    return x;
                })
                .ToList();

            results.Add((viewEntityRevision, resultingCategories));
        }

        return results;
    }
}
