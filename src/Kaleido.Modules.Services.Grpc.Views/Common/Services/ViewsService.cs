using Grpc.Core;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Create;
using Kaleido.Modules.Services.Grpc.Views.Delete;
using Kaleido.Modules.Services.Grpc.Views.Get;
using Kaleido.Modules.Services.Grpc.Views.GetAll;
using Kaleido.Modules.Services.Grpc.Views.GetAllByName;
using Kaleido.Modules.Services.Grpc.Views.GetAllRevisions;
using Kaleido.Modules.Services.Grpc.Views.GetRevision;
using Kaleido.Modules.Services.Grpc.Views.Update;

namespace Kaleido.Modules.Services.Grpc.Views.Common.Services;

public class ViewsService : GrpcViews.GrpcViewsBase
{
    private readonly ICreateHandler _createHandler;
    private readonly IDeleteHandler _deleteHandler;
    private readonly IGetHandler _getHandler;
    private readonly IGetAllHandler _getAllHandler;
    private readonly IGetAllFilteredHandler _getAllByNameHandler;
    private readonly IGetAllRevisionsHandler _getAllRevisionsHandler;
    private readonly IGetRevisionHandler _getRevisionHandler;
    private readonly IUpdateHandler _updateHandler;

    public ViewsService(
        ICreateHandler createHandler,
        IDeleteHandler deleteHandler,
        IGetHandler getHandler,
        IGetAllHandler getAllHandler,
        IGetAllFilteredHandler getAllByNameHandler,
        IGetAllRevisionsHandler getAllRevisionsHandler,
        IGetRevisionHandler getRevisionHandler,
        IUpdateHandler updateHandler
        )
    {
        _createHandler = createHandler;
        _deleteHandler = deleteHandler;
        _getHandler = getHandler;
        _getAllHandler = getAllHandler;
        _getAllByNameHandler = getAllByNameHandler;
        _getAllRevisionsHandler = getAllRevisionsHandler;
        _getRevisionHandler = getRevisionHandler;
        _updateHandler = updateHandler;
    }

    public override Task<ViewResponse> CreateView(View request, ServerCallContext context)
    {
        return _createHandler.HandleAsync(request, context.CancellationToken);
    }

    public override Task<ViewResponse> DeleteView(ViewRequest request, ServerCallContext context)
    {
        return _deleteHandler.HandleAsync(request, context.CancellationToken);
    }

    public override Task<ViewResponse> GetView(ViewRequest request, ServerCallContext context)
    {
        return _getHandler.HandleAsync(request, context.CancellationToken);
    }

    public override Task<ViewListResponse> GetAllViews(EmptyRequest request, ServerCallContext context)
    {
        return _getAllHandler.HandleAsync(request, context.CancellationToken);
    }

    public override Task<ViewListResponse> GetAllViewsFiltered(GetAllViewsFilteredRequest request, ServerCallContext context)
    {
        return _getAllByNameHandler.HandleAsync(request, context.CancellationToken);
    }

    public override Task<ViewListResponse> GetAllViewRevisions(ViewRequest request, ServerCallContext context)
    {
        return _getAllRevisionsHandler.HandleAsync(request, context.CancellationToken);
    }

    public override Task<ViewResponse> GetViewRevision(GetViewRevisionRequest request, ServerCallContext context)
    {
        return _getRevisionHandler.HandleAsync(request, context.CancellationToken);
    }

    public override Task<ViewResponse> UpdateView(ViewActionRequest request, ServerCallContext context)
    {
        return _updateHandler.HandleAsync(request, context.CancellationToken);
    }
}
