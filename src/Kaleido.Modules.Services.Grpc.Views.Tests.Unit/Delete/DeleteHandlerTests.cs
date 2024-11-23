using AutoMapper;
using Grpc.Core;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Common.Constants;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Validators;
using Kaleido.Modules.Services.Grpc.Views.Delete;
using Moq;
using Xunit;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Unit.Delete;

public class DeleteHandlerTests
{
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IDeleteManager> _deleteManagerMock;
    private readonly DeleteHandler _sut;

    public DeleteHandlerTests()
    {
        _mapperMock = new Mock<IMapper>();
        _deleteManagerMock = new Mock<IDeleteManager>();
        _sut = new DeleteHandler(_mapperMock.Object, _deleteManagerMock.Object, new KeyValidator());
    }

    [Fact]
    public async Task HandleAsync_WhenViewNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var request = new ViewRequest { Key = Guid.NewGuid().ToString() };
        _deleteManagerMock
            .Setup(x => x.DeleteAsync(Guid.Parse(request.Key), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ManagerResponse(ManagerResponseState.NotFound));

        // Act
        var act = () => _sut.HandleAsync(request);

        // Assert
        var exception = await Assert.ThrowsAsync<RpcException>(() => act());
        Assert.Equal(StatusCode.NotFound, exception.Status.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenViewExists_ReturnsResponse()
    {
        // Arrange
        var request = new ViewRequest { Key = Guid.NewGuid().ToString() };
        var viewResult = new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
        {
            Entity = new ViewEntity(),
            Revision = new ViewRevisionEntity()
        };
        var categoryLinks = new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>();

        _deleteManagerMock
            .Setup(x => x.DeleteAsync(Guid.Parse(request.Key), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ManagerResponse(viewResult, categoryLinks));

        var viewWithCategoriesResult = new EntityLifeCycleResult<ViewWithCategories, BaseRevisionEntity>
        {
            Entity = new ViewWithCategories(),
            Revision = new BaseRevisionEntity()
        };
        _mapperMock
            .Setup(x => x.Map<EntityLifeCycleResult<ViewWithCategories, BaseRevisionEntity>>(viewResult))
            .Returns(viewWithCategoriesResult);

        var expectedResponse = new ViewResponse();
        _mapperMock
            .Setup(x => x.Map<ViewResponse>(viewWithCategoriesResult))
            .Returns(expectedResponse);

        // Act
        var result = await _sut.HandleAsync(request);

        // Assert
        Assert.Equal(expectedResponse, result);
    }

    [Fact]
    public async Task HandleAsync_WhenInvalidKey_ThrowsInvalidArgumentException()
    {
        // Arrange
        var request = new ViewRequest { Key = "invalid-guid" };

        // Act
        var act = () => _sut.HandleAsync(request);

        // Assert
        var exception = await Assert.ThrowsAsync<RpcException>(() => act());
        Assert.Equal(StatusCode.InvalidArgument, exception.Status.StatusCode);
    }
}