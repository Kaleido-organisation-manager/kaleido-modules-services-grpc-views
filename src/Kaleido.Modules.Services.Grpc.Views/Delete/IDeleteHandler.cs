using Kaleido.Common.Services.Grpc.Handlers;
using Kaleido.Grpc.Views;

namespace Kaleido.Modules.Services.Grpc.Views.Delete;

public interface IDeleteHandler : IBaseHandler<ViewRequest, ViewResponse>
{
}