using AutoMapper;
using FluentValidation;
using Grpc.Core;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Validators;

namespace Kaleido.Modules.Services.Grpc.Views.GetAllByName;

public class GetAllFilteredHandler : IGetAllFilteredHandler
{
    private readonly IGetAllFilteredManager _getAllByNameManager;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllFilteredHandler> _logger;
    private readonly NameValidator _nameValidator;

    public GetAllFilteredHandler(
        IGetAllFilteredManager getAllByNameManager,
        IMapper mapper,
        ILogger<GetAllFilteredHandler> logger,
        NameValidator nameValidator
        )
    {
        _getAllByNameManager = getAllByNameManager;
        _mapper = mapper;
        _logger = logger;
        _nameValidator = nameValidator;
    }

    public async Task<ViewListResponse> HandleAsync(GetAllViewsFilteredRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _nameValidator.ValidateAndThrow(request.Name);
            var results = await _getAllByNameManager.GetAllAsync(request.Name, cancellationToken);

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
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message, ex));
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.Internal, ex.Message, ex));
        }

    }
}
