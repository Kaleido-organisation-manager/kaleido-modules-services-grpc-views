using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Kaleido.Common.Services.Grpc.Constants;
using Kaleido.Common.Services.Grpc.Handlers.Interfaces;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Kaleido.Modules.Services.Grpc.Views.GetAll;
using Moq;
using Moq.AutoMock;
using Xunit;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Unit.GetAll
{
    public class GetAllManagerTests
    {
        private readonly AutoMocker _mocker;
        private readonly Mock<IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity>> _viewLifecycleHandlerMock;
        private readonly Mock<IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>> _categoryViewLinkLifecycleHandlerMock;
        private readonly GetAllManager _sut;

        public GetAllManagerTests()
        {
            _mocker = new AutoMocker();
            _viewLifecycleHandlerMock = _mocker.GetMock<IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity>>();
            _categoryViewLinkLifecycleHandlerMock = _mocker.GetMock<IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>();
            _sut = new GetAllManager(_viewLifecycleHandlerMock.Object, _categoryViewLinkLifecycleHandlerMock.Object);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsViewsAndLinks_WhenDataExists()
        {
            // Arrange
            var viewEntities = new List<EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>>
            {
                new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
                {
                    Entity = new ViewEntity { /* Initialize properties */ },
                    Revision = new ViewRevisionEntity { /* Initialize properties */ }
                }
            };

            var categoryViewLinkResults = new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>
            {
                new EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>
                {
                    Entity = new CategoryViewLinkEntity { /* Initialize properties */ },
                    Revision = new CategoryViewLinkRevisionEntity { /* Initialize properties */ }
                }
            };

            _viewLifecycleHandlerMock
                .Setup(x => x.GetAllAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(viewEntities);

            _categoryViewLinkLifecycleHandlerMock
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<CategoryViewLinkEntity, bool>>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(categoryViewLinkResults);

            // Act
            var result = await _sut.GetAllAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal(viewEntities.First(), result.First().Item1);
            Assert.Equal(categoryViewLinkResults, result.First().Item2);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsEmptyLists_WhenNoDataExists()
        {
            // Arrange
            _viewLifecycleHandlerMock
                .Setup(x => x.GetAllAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>>());

            _categoryViewLinkLifecycleHandlerMock
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<CategoryViewLinkEntity, bool>>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>());

            // Act
            var result = await _sut.GetAllAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAsync_ThrowsException_WhenViewLifecycleHandlerFails()
        {
            // Arrange
            _viewLifecycleHandlerMock
                .Setup(x => x.GetAllAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("View lifecycle handler error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _sut.GetAllAsync(CancellationToken.None));
        }

        [Fact]
        public async Task GetAllAsync_ThrowsException_WhenCategoryViewLinkLifecycleHandlerFails()
        {

            // Arrange
            var viewEntities = new List<EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>>
            {
                new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
                {
                    Entity = new ViewEntity { /* Initialize properties */ },
                    Revision = new ViewRevisionEntity { /* Initialize properties */ }
                }
            };

            _viewLifecycleHandlerMock
                .Setup(x => x.GetAllAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(viewEntities);

            // Arrange
            _categoryViewLinkLifecycleHandlerMock
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<CategoryViewLinkEntity, bool>>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Category view link lifecycle handler error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _sut.GetAllAsync(CancellationToken.None));
        }

        [Fact]
        public async Task GetAllAsync_DoesNotIncludeDeletedViews()
        {
            // Arrange
            var viewEntities = new List<EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>>
            {
                new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
                {
                    Entity = new ViewEntity { /* Initialize properties */ },
                    Revision = new ViewRevisionEntity { Action = RevisionAction.Deleted }
                }
            };

            _viewLifecycleHandlerMock
                .Setup(x => x.GetAllAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(viewEntities);

            // Act
            var result = await _sut.GetAllAsync(CancellationToken.None);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAsync_DoesNotIncludeDeletedCategoryViewLinks()
        {
            // Arrange
            var viewEntities = new List<EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>>
            {
                new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
                {
                    Entity = new ViewEntity { /* Initialize properties */ },
                    Revision = new ViewRevisionEntity { /* Initialize properties */ }
                }
            };

            _viewLifecycleHandlerMock
                .Setup(x => x.GetAllAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(viewEntities);

            var categoryViewLinkResults = new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>
            {
                new EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>
                {
                    Entity = new CategoryViewLinkEntity { /* Initialize properties */ },
                    Revision = new CategoryViewLinkRevisionEntity { Action = RevisionAction.Deleted }
                }
            };

            _categoryViewLinkLifecycleHandlerMock
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<CategoryViewLinkEntity, bool>>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(categoryViewLinkResults);

            // Act
            var result = await _sut.GetAllAsync(CancellationToken.None);

            // Assert
            Assert.Single(result);
            Assert.Empty(result.First().Item2);
        }
    }
}
