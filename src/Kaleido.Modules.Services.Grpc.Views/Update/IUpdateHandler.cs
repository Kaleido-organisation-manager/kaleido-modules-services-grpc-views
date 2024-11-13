using Kaleido.Common.Services.Grpc.Handlers;
using Kaleido.Grpc.Views;

namespace Kaleido.Modules.Services.Grpc.Views.Update;

public interface IUpdateHandler : IBaseHandler<ViewActionRequest, ViewResponse>
{
}