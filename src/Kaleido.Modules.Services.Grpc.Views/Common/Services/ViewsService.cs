using Grpc.Core;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Create;
using Kaleido.Modules.Services.Grpc.Views.Delete;
using Kaleido.Modules.Services.Grpc.Views.Get;
using Kaleido.Modules.Services.Grpc.Views.GetAll;

namespace Kaleido.Modules.Services.Grpc.Views.Common.Services;

public class ViewsService : GrpcViews.GrpcViewsBase
{
    private readonly ICreateHandler _createHandler;
    private readonly IDeleteHandler _deleteHandler;
    private readonly IGetHandler _getHandler;
    private readonly IGetAllHandler _getAllHandler;

    public ViewsService(
        ICreateHandler createHandler,
        IDeleteHandler deleteHandler,
        IGetHandler getHandler,
        IGetAllHandler getAllHandler
        )
    {
        _createHandler = createHandler;
        _deleteHandler = deleteHandler;
        _getHandler = getHandler;
        _getAllHandler = getAllHandler;
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
}
