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
        EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>? viewResult = null;
        try
        {
            viewResult = await _viewLifecycleHandler.DeleteAsync(key, cancellationToken);
        }
        catch (RevisionNotFoundException)
        {
            return (null, Array.Empty<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>());
        }
        catch (EntityNotFoundException)
        {
            return (null, Array.Empty<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>());
        }

        var categoryViewLinks = await _categoryViewLinkLifecycleHandler.FindAllAsync(
            link => link.ViewKey == key,
            cancellationToken: cancellationToken);

        var resultLinks = new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>();
        foreach (var categoryViewLink in categoryViewLinks)
        {
            var result = await _categoryViewLinkLifecycleHandler.DeleteAsync(categoryViewLink.Key, cancellationToken);
            if (result != null)
            {
                resultLinks.Add(result);
            }
        }

        return (viewResult, resultLinks);
    }
}