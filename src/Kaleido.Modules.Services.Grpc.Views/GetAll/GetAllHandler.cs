using AutoMapper;
using Grpc.Core;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;

namespace Kaleido.Modules.Services.Grpc.Views.GetAll;

public class GetAllHandler : IGetAllHandler
{
    private readonly IGetAllManager _getAllManager;
    private readonly IMapper _mapper;

    public GetAllHandler(IGetAllManager getAllManager, IMapper mapper)
    {
        _getAllManager = getAllManager;
        _mapper = mapper;
    }

    public async Task<ViewListResponse> HandleAsync(EmptyRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await _getAllManager.GetAllAsync(cancellationToken);

            var response = new ViewListResponse();
            foreach (var (view, categoryViewLinks) in results)
            {
                var viewWithCategoriesResult = _mapper.Map<EntityLifeCycleResult<ViewWithCategories, BaseRevisionEntity>>(view);
                viewWithCategoriesResult.Entity.Categories = categoryViewLinks;
                response.Views.Add(_mapper.Map<ViewResponse>(viewWithCategoriesResult));
            }

            return response;
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.Internal, ex.Message, ex));
        }
    }
}