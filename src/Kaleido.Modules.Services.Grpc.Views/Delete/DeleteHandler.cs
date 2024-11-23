using AutoMapper;
using FluentValidation;
using Grpc.Core;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Common.Constants;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Validators;

namespace Kaleido.Modules.Services.Grpc.Views.Delete;

public class DeleteHandler : IDeleteHandler
{
    private readonly IMapper _mapper;
    private readonly IDeleteManager _deleteManager;
    private readonly KeyValidator _keyValidator;

    public DeleteHandler(
        IMapper mapper,
        IDeleteManager deleteManager,
        KeyValidator keyValidator
    )
    {
        _mapper = mapper;
        _deleteManager = deleteManager;
        _keyValidator = keyValidator;
    }

    public async Task<ViewResponse> HandleAsync(ViewRequest request, CancellationToken cancellationToken = default)
    {
        ManagerResponse? managerResponse;
        try
        {
            _keyValidator.ValidateAndThrow(request.Key);
            var key = Guid.Parse(request.Key);
            managerResponse = await _deleteManager.DeleteAsync(key, cancellationToken);
        }
        catch (ValidationException e)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid key format", e));
        }
        catch (Exception e)
        {
            throw new RpcException(new Status(StatusCode.Internal, e.Message, e));
        }

        if (managerResponse.State == ManagerResponseState.NotFound)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"View with key {request.Key} not found"));
        }

        var viewWithCategoriesResult = _mapper.Map<EntityLifeCycleResult<ViewWithCategories, BaseRevisionEntity>>(managerResponse.View);
        viewWithCategoriesResult.Entity.Categories = managerResponse.CategoryViewLinks ?? [];

        var response = _mapper.Map<ViewResponse>(viewWithCategoriesResult);
        return response;
    }
}