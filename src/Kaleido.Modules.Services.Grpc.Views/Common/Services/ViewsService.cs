using Grpc.Core;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Create;
using Kaleido.Modules.Services.Grpc.Views.Delete;
using Kaleido.Modules.Services.Grpc.Views.Get;

namespace Kaleido.Modules.Services.Grpc.Views.Common.Services;

public class ViewsService : GrpcViews.GrpcViewsBase
{
    private readonly ICreateHandler _createHandler;
    private readonly IDeleteHandler _deleteHandler;
    private readonly IGetHandler _getHandler;

    public ViewsService(
        ICreateHandler createHandler,
        IDeleteHandler deleteHandler,
        IGetHandler getHandler
        )
    {
        _createHandler = createHandler;
        _deleteHandler = deleteHandler;
        _getHandler = getHandler;
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
}
