using Kaleido.Common.Services.Grpc.Models;

namespace Kaleido.Modules.Services.Grpc.Views.Common.Models;

public class CategoryViewLinkEntity : BaseEntity
{
    public Guid CategoryKey { get; set; } = Guid.Empty;
    public Guid ViewKey { get; set; } = Guid.Empty;
}