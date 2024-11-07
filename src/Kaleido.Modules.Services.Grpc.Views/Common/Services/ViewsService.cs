using Grpc.Core;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Create;

namespace Kaleido.Modules.Services.Grpc.Views.Common.Services;

public class ViewsService : GrpcViews.GrpcViewsBase
{
    private readonly ICreateHandler _createHandler;

    public ViewsService(ICreateHandler createHandler)
    {
        _createHandler = createHandler;
    }

    public override Task<ViewResponse> CreateView(View request, ServerCallContext context)
    {
        return _createHandler.HandleAsync(request, context.CancellationToken);
    }
}
