using Kaleido.Common.Services.Grpc.Handlers;
using Kaleido.Grpc.Views;

namespace Kaleido.Modules.Services.Grpc.Views.Get;

public interface IGetHandler : IBaseHandler<ViewRequest, ViewResponse>
{
}
