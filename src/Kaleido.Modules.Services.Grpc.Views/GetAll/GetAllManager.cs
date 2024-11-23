using Kaleido.Common.Services.Grpc.Constants;
using Kaleido.Common.Services.Grpc.Handlers.Interfaces;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;

namespace Kaleido.Modules.Services.Grpc.Views.GetAll;

public class GetAllManager : IGetAllManager
{
    private readonly IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity> _viewLifecycleHandler;
    private readonly IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity> _categoryViewLinkLifecycleHandler;

    public GetAllManager(
        IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity> viewLifecycleHandler,
        IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity> categoryViewLinkLifecycleHandler)
    {
        _viewLifecycleHandler = viewLifecycleHandler;
        _categoryViewLinkLifecycleHandler = categoryViewLinkLifecycleHandler;
    }

    public async Task<IEnumerable<ManagerResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var views = await _viewLifecycleHandler.FindAllAsync(
            entity => true,
            revision => revision.Action != RevisionAction.Deleted && revision.Status == RevisionStatus.Active,
            cancellationToken: cancellationToken);

        var results = new List<ManagerResponse>();

        foreach (var view in views)
        {
            var categoryViewLinks = await _categoryViewLinkLifecycleHandler.FindAllAsync(
                link => link.ViewKey == view.Key,
                revision => revision.Action != RevisionAction.Deleted && revision.Status == RevisionStatus.Active,
                cancellationToken: cancellationToken);

            results.Add(new ManagerResponse(view, categoryViewLinks));
        }

        return results;
    }
}