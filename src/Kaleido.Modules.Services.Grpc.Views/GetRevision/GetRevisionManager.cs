using Kaleido.Common.Services.Grpc.Constants;
using Kaleido.Common.Services.Grpc.Handlers.Interfaces;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Constants;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;

namespace Kaleido.Modules.Services.Grpc.Views.GetRevision;

public class GetRevisionManager : IGetRevisionManager
{

    private readonly IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity> _viewLifecycleHandler;
    private readonly IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity> _categoryViewLinkLifecycleHandler;

    public GetRevisionManager(IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity> viewLifecycleHandler, IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity> categoryViewLinkLifecycleHandler)
    {
        _viewLifecycleHandler = viewLifecycleHandler;
        _categoryViewLinkLifecycleHandler = categoryViewLinkLifecycleHandler;
    }

    public async Task<ManagerResponse> GetViewRevision(Guid key, DateTime createdAt, CancellationToken cancellationToken)
    {
        var viewResult = await _viewLifecycleHandler.GetHistoricAsync(key, createdAt, cancellationToken);

        if (viewResult is null)
        {
            return new ManagerResponse(ManagerResponseState.NotFound);
        }

        var categoryViewLinkResults = await _categoryViewLinkLifecycleHandler.FindAllAsync(link => link.ViewKey == key, r => r.CreatedAt == createdAt, cancellationToken: cancellationToken);
        categoryViewLinkResults = categoryViewLinkResults.GroupBy(l => l.Key).Select(l => l.OrderByDescending(x => x.Revision.Revision).First()).Where(l => l.Revision.Action != RevisionAction.Deleted);

        return new ManagerResponse(viewResult, categoryViewLinkResults);
    }
}
