using Kaleido.Common.Services.Grpc.Handlers;
using Kaleido.Grpc.Views;

namespace Kaleido.Modules.Services.Grpc.Views.GetAllRevisions;

public interface IGetAllRevisionsHandler : IBaseHandler<ViewRequest, ViewListResponse>
{
}
