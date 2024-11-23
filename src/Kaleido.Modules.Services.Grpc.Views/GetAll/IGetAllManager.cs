using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;

namespace Kaleido.Modules.Services.Grpc.Views.GetAll;

public interface IGetAllManager
{

    Task<IEnumerable<ManagerResponse>> GetAllAsync(CancellationToken cancellationToken);
}
