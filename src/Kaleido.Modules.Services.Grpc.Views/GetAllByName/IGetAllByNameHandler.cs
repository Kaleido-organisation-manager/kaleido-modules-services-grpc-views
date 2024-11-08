using Kaleido.Common.Services.Grpc.Handlers;
using Kaleido.Grpc.Views;

namespace Kaleido.Modules.Services.Grpc.Views.GetAllByName;

public interface IGetAllByNameHandler : IBaseHandler<GetAllViewsByNameRequest, ViewListResponse>
{
}
