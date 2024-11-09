using Kaleido.Common.Services.Grpc.Handlers;
using Kaleido.Grpc.Views;

namespace Kaleido.Modules.Services.Grpc.Views.GetRevision;

public interface IGetRevisionHandler : IBaseHandler<GetViewRevisionRequest, ViewResponse>
{
}
