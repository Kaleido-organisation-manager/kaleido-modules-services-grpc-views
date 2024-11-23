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

    public async Task<IEnumerable<ManagerResponse>> GetAllRevisionAsync(Guid key, CancellationToken cancellationToken)
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


        var compositeRevisions = new List<ManagerResponse>();

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
                compositeRevisions.Add(new ManagerResponse(viewEntityRevision, categoryViewLinksRevisions));
            }
        }

        var results = new List<ManagerResponse>();

        for (int i = 0; i < compositeRevisions.Count; i++)
        {
            var managerResponse = compositeRevisions[i];

            EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>? previousViewEntityRevision = null;
            IEnumerable<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>? previousCategoryViewLinksRevisions = null;

            if (i < compositeRevisions.Count - 1)
            {
                var nextManagerResponse = compositeRevisions[i + 1];
                previousViewEntityRevision = nextManagerResponse.View;
                previousCategoryViewLinksRevisions = nextManagerResponse.CategoryViewLinks;
            }

            if (previousViewEntityRevision != null && previousViewEntityRevision.Revision.Revision == managerResponse.View?.Revision.Revision)
            {
                managerResponse.View.Revision.Action = RevisionAction.Unmodified;
            }

            var previousDeletedCategories = previousCategoryViewLinksRevisions?.Where(x => x.Revision.Action == RevisionAction.Deleted).ToList() ?? new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>();
            var resultingCategories = managerResponse.CategoryViewLinks?
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

            results.Add(new ManagerResponse(managerResponse.View!, resultingCategories!));
        }

        return results;
    }
}
