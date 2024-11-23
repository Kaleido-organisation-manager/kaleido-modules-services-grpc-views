using AutoMapper;
using FluentValidation;
using Grpc.Core;
using Kaleido.Common.Services.Grpc.Constants;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Grpc.Categories;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Common.Mappers;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Validators;
using Kaleido.Modules.Services.Grpc.Views.Create;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.AutoMock;
using static Kaleido.Grpc.Categories.GrpcCategories;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Unit.Create;

public class CreateHandlerTests
{
    private readonly AutoMocker _mocker;
    private readonly Mock<ICreateManager> _createManagerMock;
    private readonly Mock<GrpcCategoriesClient> _categoryClientMock;
    private readonly CreateHandler _sut;

    public CreateHandlerTests()
    {
        _mocker = new AutoMocker();


        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ViewMappingProfile>();
        });
        _mocker.Use(mapper.CreateMapper());

        _createManagerMock = _mocker.GetMock<ICreateManager>();

        _createManagerMock.Setup(m => m.CreateAsync(It.IsAny<ViewEntity>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ViewEntity entity, IEnumerable<string> categoryKeys, CancellationToken cancellationToken) => (
                new ManagerResponse(
                    new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
                    {
                        Entity = entity,
                        Revision = new ViewRevisionEntity()
                        {
                            Action = RevisionAction.Created,
                            CreatedAt = DateTime.UtcNow,
                            Key = Guid.NewGuid(),
                            EntityId = Guid.NewGuid(),
                            Id = Guid.NewGuid(),
                            Revision = 1
                        }
                    },
                    categoryKeys.Select(category => new EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>
                    {
                        Entity = new CategoryViewLinkEntity(),
                        Revision = new CategoryViewLinkRevisionEntity()
                    })
                )
                ));

        _categoryClientMock = new Mock<GrpcCategoriesClient>();
        _categoryClientMock.Setup(c => c.GetCategoryAsync(It.IsAny<CategoryRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(new AsyncUnaryCall<CategoryResponse>(Task.FromResult(new CategoryResponse()), Task.FromResult(new Metadata()), null!, null!, null!));

        _mocker.Use(_categoryClientMock.Object);

        _mocker.Use(new ViewValidator(_categoryClientMock.Object, NullLogger<ViewValidator>.Instance));

        _sut = _mocker.CreateInstance<CreateHandler>();
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_CreatesViewAndLinks()
    {
        // Arrange
        var categoryList = new List<string> { Guid.NewGuid().ToString() };
        var request = new View()
        {
            Name = "test",
            Categories = { categoryList }
        };

        // Act
        var result = await _sut.HandleAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.View);
        Assert.NotNull(result.View.Categories);
        Assert.NotEmpty(result.View.Categories);
        Assert.NotNull(result.Revision);
        Assert.Equal(request.Name, result.View.Name);

        _createManagerMock.Verify(m => m.CreateAsync(It.IsAny<ViewEntity>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ValidationFails_ThrowsValidationException()
    {
        // Arrange
        var request = new View();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RpcException>(() => _sut.HandleAsync(request));
        Assert.Equal(StatusCode.InvalidArgument, exception.Status.StatusCode);
        _createManagerMock.Verify(m => m.CreateAsync(It.IsAny<ViewEntity>(), It.IsAny<IEnumerable<string>>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        View request = null!;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RpcException>(() => _sut.HandleAsync(request!));
        Assert.Equal(StatusCode.Internal, exception.Status.StatusCode);
        _createManagerMock.Verify(m => m.CreateAsync(It.IsAny<ViewEntity>(), It.IsAny<IEnumerable<string>>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_EmptyCategories_ThrowsValidationException()
    {
        // Arrange
        var request = new View()
        {
            Name = "test",
            Categories = { }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RpcException>(() => _sut.HandleAsync(request));
        Assert.Equal(StatusCode.InvalidArgument, exception.Status.StatusCode);

        _createManagerMock.Verify(m => m.CreateAsync(It.IsAny<ViewEntity>(), It.IsAny<IEnumerable<string>>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_CreatesViewWithCorrectCategoryLinks()
    {
        // Arrange
        var categoryList = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
        var request = new View()
        {
            Name = "test",
            Categories = { categoryList }
        };

        // Act
        var result = await _sut.HandleAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.View);
        Assert.NotEmpty(result.View.Categories);
        _createManagerMock.Verify(m => m.CreateAsync(It.IsAny<ViewEntity>(), It.IsAny<IEnumerable<string>>(), default), Times.Once);
    }
}