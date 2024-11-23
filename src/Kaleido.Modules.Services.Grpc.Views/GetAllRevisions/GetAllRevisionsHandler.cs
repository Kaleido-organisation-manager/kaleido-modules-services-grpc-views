using AutoMapper;
using FluentValidation;
using Grpc.Core;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Grpc.Categories;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Validators;

namespace Kaleido.Modules.Services.Grpc.Views.GetAllRevisions;

public class GetAllRevisionsHandler : IGetAllRevisionsHandler
{
    private readonly IGetAllRevisionsManager _getAllRevisionsManager;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllRevisionsHandler> _logger;
    private readonly KeyValidator _validator;


    public GetAllRevisionsHandler(
        IGetAllRevisionsManager getAllRevisionsManager,
        IMapper mapper,
        ILogger<GetAllRevisionsHandler> logger,
        KeyValidator validator
        )
    {
        _getAllRevisionsManager = getAllRevisionsManager;
        _mapper = mapper;
        _logger = logger;
        _validator = validator;
    }

    public async Task<ViewListResponse> HandleAsync(ViewRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _validator.ValidateAndThrow(request.Key);

            var key = Guid.Parse(request.Key);
            var results = await _getAllRevisionsManager.GetAllRevisionAsync(key, cancellationToken);

            var response = new ViewListResponse();
            foreach (var managerResponse in results)
            {
                var viewWithCategoriesResult = _mapper.Map<EntityLifeCycleResult<ViewWithCategories, BaseRevisionEntity>>(managerResponse.View);
                viewWithCategoriesResult.Entity.Categories = managerResponse.CategoryViewLinks ?? [];
                response.Views.Add(_mapper.Map<ViewResponse>(viewWithCategoriesResult));
            }

            return response;
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex, "Validation error");
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all revisions");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message, ex));
        }
    }
}
