using Kaleido.Common.Services.Grpc.Constants;
using Kaleido.Common.Services.Grpc.Handlers.Interfaces;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;

namespace Kaleido.Modules.Services.Grpc.Views.GetAllByName;

public class GetAllByNameManager : IGetAllByNameManager
{
    private readonly IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity> _viewLifecycleHandler;
    private readonly IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity> _categoryViewLinkLifecycleHandler;

    public GetAllByNameManager(
        IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity> viewLifecycleHandler,
        IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity> categoryViewLinkLifecycleHandler
        )
    {
        _viewLifecycleHandler = viewLifecycleHandler;
        _categoryViewLinkLifecycleHandler = categoryViewLinkLifecycleHandler;
    }

    public async Task<IEnumerable<(EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>, IEnumerable<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>)>> GetAllAsync(string name, CancellationToken cancellationToken)
    {
        var views = await _viewLifecycleHandler.FindAllAsync(v => v.Name.ToLower().Contains(name.ToLower()), cancellationToken: cancellationToken);
        views = views.Where(v => v.Revision.Action != RevisionAction.Deleted);

        var results = new List<(EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>, IEnumerable<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>)>();

        foreach (var view in views)
        {
            var categoryViewLinks = await _categoryViewLinkLifecycleHandler.FindAllAsync(link => link.ViewKey == view.Key, cancellationToken: cancellationToken);
            categoryViewLinks = categoryViewLinks.Where(l => l.Revision.Action != RevisionAction.Deleted);

            results.Add((view, categoryViewLinks));
        }

        return results;
    }
}
