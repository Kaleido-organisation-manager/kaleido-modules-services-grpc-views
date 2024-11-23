using System.Linq.Expressions;
using Kaleido.Common.Services.Grpc.Exceptions;
using Kaleido.Common.Services.Grpc.Handlers.Interfaces;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Constants;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Kaleido.Modules.Services.Grpc.Views.Delete;
using Moq;
using Moq.AutoMock;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Unit.Delete;

public class DeleteManagerTests
{
    private readonly AutoMocker _mocker;
    private readonly DeleteManager _sut;

    public DeleteManagerTests()
    {
        _mocker = new AutoMocker();
        _sut = _mocker.CreateInstance<DeleteManager>();
    }

    [Fact]
    public async Task DeleteAsync_WhenViewDoesNotExist_ReturnsNull()
    {
        // Arrange
        var key = Guid.NewGuid();
        var viewLifecycleHandlerMock = _mocker.GetMock<IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity>>();
        viewLifecycleHandlerMock
            .Setup(x => x.DeleteAsync(key, It.IsAny<ViewRevisionEntity?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EntityNotFoundException("Not found"));

        // Act
        var result = await _sut.DeleteAsync(key);

        // Assert
        Assert.Equal(ManagerResponseState.NotFound, result.State);
    }

    [Fact]
    public async Task DeleteAsync_WhenViewExists_DeletesViewAndLinks()
    {
        // Arrange
        var key = Guid.NewGuid();
        var viewResult = new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
        {
            Entity = new ViewEntity(),
            Revision = new ViewRevisionEntity()
            {
                Key = key,
            }
        };

        var categoryViewLinks = new List<CategoryViewLinkEntity>
        {
            new() { ViewKey = key },
            new() { ViewKey = key }
        };

        var categoryViewLinkResults = categoryViewLinks.Select(link => new EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>
        {
            Entity = link,
            Revision = new CategoryViewLinkRevisionEntity()
            {
                Key = Guid.NewGuid(),
            }
        }).ToList();

        var viewLifecycleHandlerMock = _mocker.GetMock<IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity>>();
        viewLifecycleHandlerMock
            .Setup(x => x.DeleteAsync(key, It.IsAny<ViewRevisionEntity?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(viewResult);

        viewLifecycleHandlerMock
            .Setup(x => x.GetAsync(key, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(viewResult);

        var categoryViewLinkLifecycleHandlerMock = _mocker.GetMock<IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>();
        categoryViewLinkLifecycleHandlerMock
            .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<CategoryViewLinkEntity, bool>>>(),
                It.IsAny<Expression<Func<CategoryViewLinkRevisionEntity, bool>>>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(categoryViewLinkResults);

        categoryViewLinkLifecycleHandlerMock
            .Setup(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CategoryViewLinkRevisionEntity?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid linkKey, CategoryViewLinkRevisionEntity? _, CancellationToken _) =>
                categoryViewLinkResults.First(r => r.Key == linkKey));

        // Act
        var result = await _sut.DeleteAsync(key);

        // Assert
        Assert.NotNull(result.View);
        Assert.Equal(viewResult, result.View);
        Assert.Equal(2, result.CategoryViewLinks!.Count());
        Assert.Equal(categoryViewLinkResults, result.CategoryViewLinks);
    }
}