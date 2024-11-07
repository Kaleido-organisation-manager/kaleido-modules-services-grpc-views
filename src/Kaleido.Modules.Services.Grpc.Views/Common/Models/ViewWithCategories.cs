using Kaleido.Common.Services.Grpc.Models;

namespace Kaleido.Modules.Services.Grpc.Views.Common.Models;

public class ViewWithCategories : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public IEnumerable<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>> Categories { get; set; } = [];
}