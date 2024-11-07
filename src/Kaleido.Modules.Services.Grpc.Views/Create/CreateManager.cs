using Kaleido.Common.Services.Grpc.Handlers.Interfaces;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;

namespace Kaleido.Modules.Services.Grpc.Views.Create;

public class CreateManager : ICreateManager
{
    private readonly IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity> _viewLifecycleHandler;
    private readonly IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity> _categoryViewLinkLifecycleHandler;

    public CreateManager(
        IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity> viewLifecycleHandler,
        IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity> categoryViewLinkLifecycleHandler
    )
    {
        _viewLifecycleHandler = viewLifecycleHandler;
        _categoryViewLinkLifecycleHandler = categoryViewLinkLifecycleHandler;
    }

    public async Task<(EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>, IEnumerable<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>)> CreateAsync(ViewEntity viewEntity, IEnumerable<string> categoryKeys, CancellationToken cancellationToken = default)
    {
        var viewResult = await _viewLifecycleHandler.CreateAsync(viewEntity, cancellationToken);

        var categoryViewLinks = categoryKeys.Select(category => new CategoryViewLinkEntity
        {
            CategoryKey = Guid.Parse(category),
            ViewKey = viewResult.Key
        });

        var resultLinks = new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>();
        foreach (var categoryViewLink in categoryViewLinks)
        {
            var result = await _categoryViewLinkLifecycleHandler.CreateAsync(categoryViewLink, cancellationToken); ;
            resultLinks.Add(result);
        }

        return (viewResult, resultLinks);
    }
}