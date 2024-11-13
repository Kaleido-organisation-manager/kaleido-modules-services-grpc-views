using AutoMapper;
using FluentValidation;
using Grpc.Core;
using Kaleido.Common.Services.Grpc.Exceptions;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Validators;

namespace Kaleido.Modules.Services.Grpc.Views.Get;

public class GetHandler : IGetHandler
{
    private readonly IGetManager _getManager;
    private readonly IMapper _mapper;
    private readonly ILogger<GetHandler> _logger;
    private readonly KeyValidator _keyValidator;

    public GetHandler(
        IGetManager getManager,
        IMapper mapper,
        ILogger<GetHandler> logger,
        KeyValidator keyValidator
        )
    {
        _getManager = getManager;
        _mapper = mapper;
        _logger = logger;
        _keyValidator = keyValidator;
    }

    public async Task<ViewResponse> HandleAsync(ViewRequest request, CancellationToken cancellationToken = default)
    {

        EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>? viewResult = null;
        IEnumerable<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>> categoryViewLinkResults = Enumerable.Empty<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>();

        try
        {
            _keyValidator.ValidateAndThrow(request.Key);
            var key = Guid.Parse(request.Key);
            (viewResult, categoryViewLinkResults) = await _getManager.GetAsync(key, cancellationToken);
        }
        catch (FluentValidation.ValidationException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message, ex));
        }
        catch (RevisionNotFoundException)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "View revision not found"));
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.Internal, ex.Message, ex));
        }

        if (viewResult is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "View not found"));
        }

        var viewWithCategoriesResult = _mapper.Map<EntityLifeCycleResult<ViewWithCategories, BaseRevisionEntity>>(viewResult);
        viewWithCategoriesResult.Entity.Categories = categoryViewLinkResults;

        var response = _mapper.Map<ViewResponse>(viewWithCategoriesResult);
        return response;
    }
}
