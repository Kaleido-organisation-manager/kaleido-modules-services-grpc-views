using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Kaleido.Common.Services.Grpc.Handlers.Interfaces;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Constants;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Kaleido.Modules.Services.Grpc.Views.GetRevision;
using Moq;
using Moq.AutoMock;
using Xunit;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Unit.GetRevision
{
    public class GetRevisionManagerTests
    {
        private readonly AutoMocker _mocker;
        private readonly Mock<IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity>> _viewLifecycleHandlerMock;
        private readonly Mock<IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>> _categoryViewLinkLifecycleHandlerMock;
        private readonly GetRevisionManager _sut;

        public GetRevisionManagerTests()
        {
            _mocker = new AutoMocker();
            _viewLifecycleHandlerMock = _mocker.GetMock<IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity>>();
            _categoryViewLinkLifecycleHandlerMock = _mocker.GetMock<IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>();
            _sut = new GetRevisionManager(_viewLifecycleHandlerMock.Object, _categoryViewLinkLifecycleHandlerMock.Object);
        }

        [Fact]
        public async Task GetViewRevision_ReturnsViewAndLinks_WhenDataExists()
        {
            // Arrange
            var key = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;
            var viewResult = new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
            {
                Entity = new ViewEntity(),
                Revision = new ViewRevisionEntity { CreatedAt = createdAt }
            };

            var categoryViewLinkResults = new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>
            {
                new EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>
                {
                    Entity = new CategoryViewLinkEntity(),
                    Revision = new CategoryViewLinkRevisionEntity { CreatedAt = createdAt }
                }
            };

            _viewLifecycleHandlerMock
                .Setup(x => x.GetHistoricAsync(key, createdAt, It.IsAny<CancellationToken>()))
                .ReturnsAsync(viewResult);

            _categoryViewLinkLifecycleHandlerMock
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<CategoryViewLinkEntity, bool>>>(), It.IsAny<Expression<Func<CategoryViewLinkRevisionEntity, bool>>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(categoryViewLinkResults);

            // Act
            var result = await _sut.GetViewRevision(key, createdAt, CancellationToken.None);

            // Assert
            Assert.NotNull(result.View);
            Assert.Equal(viewResult, result.View);
            Assert.Equal(categoryViewLinkResults, result.CategoryViewLinks);
        }

        [Fact]
        public async Task GetViewRevision_ReturnsNotFound_WhenViewDoesNotExist()
        {
            // Arrange
            var key = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;

            _viewLifecycleHandlerMock
                .Setup(x => x.GetHistoricAsync(key, createdAt, It.IsAny<CancellationToken>()))
                .ReturnsAsync((EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>?)null);

            // Act
            var result = await _sut.GetViewRevision(key, createdAt, CancellationToken.None);

            // Assert
            Assert.Equal(ManagerResponseState.NotFound, result.State);
            Assert.Null(result.View);
            Assert.Null(result.CategoryViewLinks);
        }

        [Fact]
        public async Task GetViewRevision_ThrowsException_WhenViewLifecycleHandlerFails()
        {
            // Arrange
            var key = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;

            _viewLifecycleHandlerMock
                .Setup(x => x.GetHistoricAsync(key, createdAt, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("View lifecycle handler error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _sut.GetViewRevision(key, createdAt, CancellationToken.None));
        }

        [Fact]
        public async Task GetViewRevision_ThrowsException_WhenCategoryViewLinkLifecycleHandlerFails()
        {
            // Arrange
            var key = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;
            var viewResult = new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
            {
                Entity = new ViewEntity(),
                Revision = new ViewRevisionEntity { CreatedAt = createdAt }
            };

            _viewLifecycleHandlerMock
                .Setup(x => x.GetHistoricAsync(key, createdAt, It.IsAny<CancellationToken>()))
                .ReturnsAsync(viewResult);

            _categoryViewLinkLifecycleHandlerMock
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<CategoryViewLinkEntity, bool>>>(), It.IsAny<Expression<Func<CategoryViewLinkRevisionEntity, bool>>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Category view link lifecycle handler error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _sut.GetViewRevision(key, createdAt, CancellationToken.None));
        }
    }
}
