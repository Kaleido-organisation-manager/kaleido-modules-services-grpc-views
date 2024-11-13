using Kaleido.Common.Services.Grpc.Constants;
using Kaleido.Common.Services.Grpc.Exceptions;
using Kaleido.Common.Services.Grpc.Handlers.Interfaces;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;

namespace Kaleido.Modules.Services.Grpc.Views.Delete;

public class DeleteManager : IDeleteManager
{
    private readonly IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity> _viewLifecycleHandler;
    private readonly IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity> _categoryViewLinkLifecycleHandler;

    public DeleteManager(
        IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity> viewLifecycleHandler,
        IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity> categoryViewLinkLifecycleHandler
    )
    {
        _viewLifecycleHandler = viewLifecycleHandler;
        _categoryViewLinkLifecycleHandler = categoryViewLinkLifecycleHandler;
    }

    public async Task<(EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>?, IEnumerable<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>)> DeleteAsync(
        Guid key,
        CancellationToken cancellationToken = default)
    {
        var requestedView = await _viewLifecycleHandler.GetAsync(key, cancellationToken: cancellationToken);

        if (requestedView == null)
        {
            return (null, Array.Empty<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>());
        }

        var categoryViewLinks = await _categoryViewLinkLifecycleHandler.FindAllAsync(
            link => link.ViewKey == key,
            cancellationToken: cancellationToken);

        categoryViewLinks = categoryViewLinks.GroupBy(l => l.Key).Select(l => l.OrderByDescending(x => x.Revision.Revision).First()).Where(l => l.Revision.Action != RevisionAction.Deleted);

        var timestamp = DateTime.UtcNow;

        var resultLinks = new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>();
        foreach (var categoryViewLink in categoryViewLinks)
        {
            var categoryRevision = new CategoryViewLinkRevisionEntity
            {
                Key = categoryViewLink.Key,
                CreatedAt = timestamp
            };

            var result = await _categoryViewLinkLifecycleHandler.DeleteAsync(categoryViewLink.Key, categoryRevision, cancellationToken: cancellationToken);
            if (result != null)
            {
                resultLinks.Add(result);
            }
        }

        var viewRevision = new ViewRevisionEntity
        {
            Key = key,
            CreatedAt = timestamp
        };
        var viewResult = await _viewLifecycleHandler.DeleteAsync(key, viewRevision, cancellationToken: cancellationToken);

        return (viewResult, resultLinks);
    }
}