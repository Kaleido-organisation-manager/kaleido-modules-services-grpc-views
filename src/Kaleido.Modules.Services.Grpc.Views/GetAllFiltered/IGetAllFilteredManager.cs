using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;

namespace Kaleido.Modules.Services.Grpc.Views.GetAllByName;

public interface IGetAllFilteredManager
{
    Task<IEnumerable<ManagerResponse>> GetAllAsync(string name, CancellationToken cancellationToken);
}
