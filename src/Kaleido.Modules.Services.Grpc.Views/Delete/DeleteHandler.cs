using AutoMapper;
using Grpc.Core;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;

namespace Kaleido.Modules.Services.Grpc.Views.Delete;

public class DeleteHandler : IDeleteHandler
{
    private readonly IMapper _mapper;
    private readonly IDeleteManager _deleteManager;

    public DeleteHandler(
        IMapper mapper,
        IDeleteManager deleteManager
    )
    {
        _mapper = mapper;
        _deleteManager = deleteManager;
    }

    public async Task<ViewResponse> HandleAsync(ViewRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = Guid.Parse(request.Key);
            var (viewResult, resultLinks) = await _deleteManager.DeleteAsync(key, cancellationToken);

            if (viewResult == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"View with key {request.Key} not found"));
            }

            var viewWithCategoriesResult = _mapper.Map<EntityLifeCycleResult<ViewWithCategories, BaseRevisionEntity>>(viewResult);
            viewWithCategoriesResult.Entity.Categories = resultLinks;

            var response = _mapper.Map<ViewResponse>(viewWithCategoriesResult);
            return response;
        }
        catch (FormatException e)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid key format", e));
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new RpcException(new Status(StatusCode.Internal, e.Message, e));
        }
    }
}