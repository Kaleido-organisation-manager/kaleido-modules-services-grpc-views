using AutoMapper;
using FluentValidation;
using Grpc.Core;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Common.Constants;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Validators;

namespace Kaleido.Modules.Services.Grpc.Views.GetRevision;

public class GetRevisionHandler : IGetRevisionHandler
{
    private readonly IGetRevisionManager _getRevisionManager;
    private readonly IMapper _mapper;
    private readonly ILogger<GetRevisionHandler> _logger;
    private readonly KeyValidator _keyValidator;

    public GetRevisionHandler(
        IGetRevisionManager getRevisionManager,
        IMapper mapper,
        ILogger<GetRevisionHandler> logger,
        KeyValidator keyValidator
        )
    {
        _getRevisionManager = getRevisionManager;
        _mapper = mapper;
        _logger = logger;
        _keyValidator = keyValidator;
    }

    public async Task<ViewResponse> HandleAsync(GetViewRevisionRequest request, CancellationToken cancellationToken)
    {
        ManagerResponse managerResponse;

        try
        {
            _keyValidator.ValidateAndThrow(request.Key);
            var key = Guid.Parse(request.Key);
            managerResponse = await _getRevisionManager.GetViewRevision(key, request.CreatedAt.ToDateTime(), cancellationToken);
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex, "Validation error getting view revision");
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message, ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting view revision");
            throw new RpcException(new Status(StatusCode.Internal, "Error getting view revision", ex));
        }


        if (managerResponse.State == ManagerResponseState.NotFound)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "View not found"));
        }

        var viewWithCategoriesResult = _mapper.Map<EntityLifeCycleResult<ViewWithCategories, BaseRevisionEntity>>(managerResponse.View);
        viewWithCategoriesResult.Entity.Categories = managerResponse.CategoryViewLinks ?? [];

        var response = _mapper.Map<ViewResponse>(viewWithCategoriesResult);
        return response;

    }
}
