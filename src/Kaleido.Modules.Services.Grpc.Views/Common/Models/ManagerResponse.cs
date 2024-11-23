using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Constants;

namespace Kaleido.Modules.Services.Grpc.Views.Common.Models;

public class ManagerResponse
{
    public readonly ManagerResponseState State;
    public readonly EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>? View;
    public readonly IEnumerable<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>? CategoryViewLinks;

    public ManagerResponse(EntityLifeCycleResult<ViewEntity, ViewRevisionEntity> view, IEnumerable<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>> categoryViewLinks)
    {
        View = view;
        CategoryViewLinks = categoryViewLinks;
        State = ManagerResponseState.Success;
    }

    public ManagerResponse(ManagerResponseState state)
    {
        State = state;
        View = null;
        CategoryViewLinks = null;
    }
}
