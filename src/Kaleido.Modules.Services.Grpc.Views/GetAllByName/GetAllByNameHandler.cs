using AutoMapper;
using Grpc.Core;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Validators;

namespace Kaleido.Modules.Services.Grpc.Views.GetAllByName;

public class GetAllByNameHandler : IGetAllByNameHandler
{
    private readonly IGetAllByNameManager _getAllByNameManager;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllByNameHandler> _logger;
    private readonly NameValidator _nameValidator;

    public GetAllByNameHandler(
        IGetAllByNameManager getAllByNameManager,
        IMapper mapper,
        ILogger<GetAllByNameHandler> logger,
        NameValidator nameValidator
        )
    {
        _getAllByNameManager = getAllByNameManager;
        _mapper = mapper;
        _logger = logger;
        _nameValidator = nameValidator;
    }

    public async Task<ViewListResponse> HandleAsync(GetAllViewsByNameRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await _getAllByNameManager.GetAllAsync(request.Name, cancellationToken);

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
