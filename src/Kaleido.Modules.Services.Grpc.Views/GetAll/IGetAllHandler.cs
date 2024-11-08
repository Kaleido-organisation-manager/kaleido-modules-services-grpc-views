using Kaleido.Common.Services.Grpc.Handlers;
using Kaleido.Grpc.Views;

namespace Kaleido.Modules.Services.Grpc.Views.GetAll;

public interface IGetAllHandler : IBaseHandler<EmptyRequest, ViewListResponse>
{
}