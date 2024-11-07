using Kaleido.Common.Services.Grpc.Models;

namespace Kaleido.Modules.Services.Grpc.Views.Common.Models;

public class ViewEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}