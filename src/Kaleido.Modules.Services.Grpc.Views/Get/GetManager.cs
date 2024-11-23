using Kaleido.Common.Services.Grpc.Constants;
using Kaleido.Common.Services.Grpc.Handlers.Interfaces;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Constants;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;

namespace Kaleido.Modules.Services.Grpc.Views.Get;

public class GetManager : IGetManager
{
    private readonly IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity> _viewLifecycleHandler;
    private readonly IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity> _categoryViewLinkLifecycleHandler;

    public GetManager(
        IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity> viewLifecycleHandler,
        IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity> categoryViewLinkLifecycleHandler)
    {
        _viewLifecycleHandler = viewLifecycleHandler;
        _categoryViewLinkLifecycleHandler = categoryViewLinkLifecycleHandler;
    }

    public async Task<ManagerResponse> GetAsync(Guid key, CancellationToken cancellationToken)
    {
        var viewResult = await _viewLifecycleHandler.GetAsync(key, cancellationToken: cancellationToken);

        if (viewResult is null || viewResult.Revision?.Action == RevisionAction.Deleted)
        {
            return new ManagerResponse(ManagerResponseState.NotFound);
        }

        var categoryViewLinkResults = await _categoryViewLinkLifecycleHandler.FindAllAsync(
            link => link.ViewKey == key,
            revision => revision.Action != RevisionAction.Deleted && revision.Status == RevisionStatus.Active,
            cancellationToken: cancellationToken);

        return new ManagerResponse(viewResult, categoryViewLinkResults);
    }
}