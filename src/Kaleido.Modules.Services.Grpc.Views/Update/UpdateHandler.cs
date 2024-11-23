using AutoMapper;
using FluentValidation;
using Grpc.Core;
using Kaleido.Common.Services.Grpc.Exceptions;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Validators;

namespace Kaleido.Modules.Services.Grpc.Views.Update;

public class UpdateHandler : IUpdateHandler
{
    private readonly IUpdateManager _updateManager;
    private readonly IMapper _mapper;
    private readonly KeyValidator _keyValidator;
    private readonly ViewValidator _viewValidator;

    public UpdateHandler(
        IUpdateManager updateManager,
        IMapper mapper,
        KeyValidator keyValidator,
        ViewValidator viewValidator)
    {
        _updateManager = updateManager;
        _mapper = mapper;
        _keyValidator = keyValidator;
        _viewValidator = viewValidator;
    }

    public async Task<ViewResponse> HandleAsync(ViewActionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _keyValidator.ValidateAndThrow(request.Key);
            await _viewValidator.ValidateAndThrowAsync(request.View, cancellationToken);

            var key = Guid.Parse(request.Key);
            var viewEntity = _mapper.Map<ViewEntity>(request.View);
            var managerResponse = await _updateManager.UpdateAsync(key, viewEntity, request.View.Categories, cancellationToken);

            var viewWithCategoriesResult = _mapper.Map<EntityLifeCycleResult<ViewWithCategories, BaseRevisionEntity>>(managerResponse.View);
            viewWithCategoriesResult.Entity.Categories = managerResponse.CategoryViewLinks ?? [];

            var response = _mapper.Map<ViewResponse>(viewWithCategoriesResult);
            return response;
        }
        catch (FluentValidation.ValidationException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message, ex));
        }
        catch (Exception ex) when (ex is EntityNotFoundException or RevisionNotFoundException)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message, ex));
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.Internal, ex.Message, ex));
        }
    }
}