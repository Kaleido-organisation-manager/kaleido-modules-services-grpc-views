using Grpc.Core;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Create;
using Kaleido.Modules.Services.Grpc.Views.Delete;
using Kaleido.Modules.Services.Grpc.Views.Get;
using Kaleido.Modules.Services.Grpc.Views.GetAll;
using Kaleido.Modules.Services.Grpc.Views.GetAllByName;

namespace Kaleido.Modules.Services.Grpc.Views.Common.Services;

public class ViewsService : GrpcViews.GrpcViewsBase
{
    private readonly ICreateHandler _createHandler;
    private readonly IDeleteHandler _deleteHandler;
    private readonly IGetHandler _getHandler;
    private readonly IGetAllHandler _getAllHandler;
    private readonly IGetAllByNameHandler _getAllByNameHandler;

    public ViewsService(
        ICreateHandler createHandler,
        IDeleteHandler deleteHandler,
        IGetHandler getHandler,
        IGetAllHandler getAllHandler,
        IGetAllByNameHandler getAllByNameHandler
        )
    {
        _createHandler = createHandler;
        _deleteHandler = deleteHandler;
        _getHandler = getHandler;
        _getAllHandler = getAllHandler;
        _getAllByNameHandler = getAllByNameHandler;
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

    public override Task<ViewListResponse> GetAllViewsByName(GetAllViewsByNameRequest request, ServerCallContext context)
    {
        return _getAllByNameHandler.HandleAsync(request, context.CancellationToken);
    }
}
