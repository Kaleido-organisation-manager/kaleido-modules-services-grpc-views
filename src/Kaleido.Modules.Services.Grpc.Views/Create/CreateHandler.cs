using System.Text.Json;
using AutoMapper;
using FluentValidation;
using Grpc.Core;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Common.Mappers;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Validators;

namespace Kaleido.Modules.Services.Grpc.Views.Create;

public class CreateHandler : ICreateHandler
{
    private readonly IMapper _mapper;
    private readonly ViewValidator _validator;
    private readonly ICreateManager _createManager;

    public CreateHandler(
        IMapper mapper,
        ViewValidator validator,
        ICreateManager createManager
    )
    {
        _mapper = mapper;
        _validator = validator;
        _createManager = createManager;
    }

    public async Task<ViewResponse> HandleAsync(View request, CancellationToken cancellationToken = default)
    {
        try
        {
            await _validator.ValidateAndThrowAsync(request, cancellationToken);
            var view = _mapper.Map<ViewEntity>(request);

            var (viewResult, resultLinks) = await _createManager.CreateAsync(view, request.Categories, cancellationToken);

            var viewWithCategoriesResult = _mapper.Map<EntityLifeCycleResult<ViewWithCategories, BaseRevisionEntity>>(viewResult);
            viewWithCategoriesResult.Entity.Categories = resultLinks;

            var response = _mapper.Map<ViewResponse>(viewWithCategoriesResult);
            return response;
        }
        catch (ValidationException e)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, e.Message, e));
        }
        catch (Exception e)
        {
            throw new RpcException(new Status(StatusCode.Internal, e.Message, e));
        }
    }
}