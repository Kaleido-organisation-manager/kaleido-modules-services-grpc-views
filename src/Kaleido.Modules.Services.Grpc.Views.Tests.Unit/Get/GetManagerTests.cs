using System.Linq.Expressions;
using Kaleido.Common.Services.Grpc.Constants;
using Kaleido.Common.Services.Grpc.Handlers.Interfaces;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Constants;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Kaleido.Modules.Services.Grpc.Views.Get;
using Moq;
using Moq.AutoMock;
using Xunit;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Unit.Get
{
    public class GetManagerTests
    {
        private readonly AutoMocker _mocker;
        private readonly Mock<IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity>> _viewLifecycleHandlerMock;
        private readonly Mock<IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>> _categoryViewLinkLifecycleHandlerMock;
        private readonly GetManager _sut;

        public GetManagerTests()
        {
            _mocker = new AutoMocker();
            _viewLifecycleHandlerMock = _mocker.GetMock<IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity>>();
            _categoryViewLinkLifecycleHandlerMock = _mocker.GetMock<IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>();
            _sut = _mocker.CreateInstance<GetManager>();
        }

        [Fact]
        public async Task GetAsync_ValidKey_ReturnsViewAndLinks()
        {
            // Arrange
            var key = Guid.NewGuid();
            var viewEntity = new ViewEntity { /* Initialize properties */ };
            var viewRevisionEntity = new ViewRevisionEntity { Action = RevisionAction.Created, Key = key };
            var categoryViewLinkResults = new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>
            {
                new EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>
                {
                    Entity = new CategoryViewLinkEntity { ViewKey = key },
                    Revision = new CategoryViewLinkRevisionEntity { Key = Guid.NewGuid() }
                }
            };

            var viewResult = new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
            {
                Entity = viewEntity,
                Revision = viewRevisionEntity
            };

            _viewLifecycleHandlerMock
                .Setup(x => x.GetAsync(key, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(viewResult);

            _categoryViewLinkLifecycleHandlerMock
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<CategoryViewLinkEntity, bool>>>(),
                    It.IsAny<Expression<Func<CategoryViewLinkRevisionEntity, bool>>>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(categoryViewLinkResults);

            // Act
            var result = await _sut.GetAsync(key, CancellationToken.None);

            // Assert
            Assert.NotNull(result.View);
            Assert.Equal(viewResult, result.View);
            Assert.Equal(categoryViewLinkResults.Count, result.CategoryViewLinks!.Count());
        }

        [Fact]
        public async Task GetAsync_DeletedView_ReturnsNotFound()
        {
            // Arrange
            var key = Guid.NewGuid();
            var viewRevisionEntity = new ViewRevisionEntity { Action = RevisionAction.Deleted };

            _viewLifecycleHandlerMock
                .Setup(x => x.GetAsync(key, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>?)null);

            // Act
            var result = await _sut.GetAsync(key, CancellationToken.None);

            // Assert
            Assert.Equal(ManagerResponseState.NotFound, result.State);
        }

        [Fact]
        public async Task GetAsync_ViewNotFound_ReturnsNotFound()
        {
            // Arrange
            var key = Guid.NewGuid();
            _viewLifecycleHandlerMock
                .Setup(x => x.GetAsync(key, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>?)null);

            // Act
            var result = await _sut.GetAsync(key, CancellationToken.None);

            // Assert
            Assert.Equal(ManagerResponseState.NotFound, result.State);
            Assert.Null(result.View);
            Assert.Null(result.CategoryViewLinks);
        }
    }
}
