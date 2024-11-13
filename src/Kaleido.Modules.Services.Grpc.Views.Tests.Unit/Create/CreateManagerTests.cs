using Kaleido.Common.Services.Grpc.Handlers.Interfaces;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Kaleido.Modules.Services.Grpc.Views.Create;
using Moq;
using Moq.AutoMock;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Unit.Create
{
    public class CreateManagerTests
    {
        private readonly AutoMocker _mocker;
        private readonly Mock<IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity>> _viewLifecycleHandlerMock;
        private readonly Mock<IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>> _categoryViewLinkLifecycleHandlerMock;
        private readonly CreateManager _sut;

        public CreateManagerTests()
        {
            _mocker = new AutoMocker();
            _viewLifecycleHandlerMock = _mocker.GetMock<IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity>>();
            _categoryViewLinkLifecycleHandlerMock = _mocker.GetMock<IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>();
            _sut = new CreateManager(_viewLifecycleHandlerMock.Object, _categoryViewLinkLifecycleHandlerMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ValidViewEntity_CreatesViewAndLinks()
        {
            // Arrange
            var viewEntity = new ViewEntity { Name = "Test View" };
            var categoryKeys = new List<string> { Guid.NewGuid().ToString() };
            var viewResult = new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
            {
                Entity = viewEntity,
                Revision = new ViewRevisionEntity { Key = Guid.NewGuid() }
            };

            _viewLifecycleHandlerMock
                .Setup(x => x.CreateAsync(viewEntity, It.IsAny<ViewRevisionEntity?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(viewResult);

            var categoryViewLinkResults = new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>();
            foreach (var categoryKey in categoryKeys)
            {
                var categoryViewLinkEntity = new CategoryViewLinkEntity { CategoryKey = Guid.Parse(categoryKey), ViewKey = viewResult.Key };
                var categoryViewLinkResult = new EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>
                {
                    Entity = categoryViewLinkEntity,
                    Revision = new CategoryViewLinkRevisionEntity { Key = Guid.NewGuid() }
                };
                categoryViewLinkResults.Add(categoryViewLinkResult);
                _categoryViewLinkLifecycleHandlerMock
                    .Setup(x => x.CreateAsync(categoryViewLinkEntity, It.IsAny<CategoryViewLinkRevisionEntity?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(categoryViewLinkResult);
            }

            // Act
            var result = await _sut.CreateAsync(viewEntity, categoryKeys);

            // Assert
            Assert.NotNull(result.Item1);
            Assert.NotNull(result.Item2);
            Assert.Equal(viewResult, result.Item1);
            Assert.Equal(categoryViewLinkResults, result.Item2);
            _viewLifecycleHandlerMock.Verify(x => x.CreateAsync(viewEntity, It.IsAny<ViewRevisionEntity?>(), It.IsAny<CancellationToken>()), Times.Once);
            _categoryViewLinkLifecycleHandlerMock.Verify(x => x.CreateAsync(It.IsAny<CategoryViewLinkEntity>(), It.IsAny<CategoryViewLinkRevisionEntity?>(), It.IsAny<CancellationToken>()), Times.Exactly(categoryKeys.Count));
        }

        [Fact]
        public async Task CreateAsync_ViewCreationFails_ThrowsException()
        {
            // Arrange
            var viewEntity = new ViewEntity { Name = "Test View" };
            var categoryKeys = new List<string> { Guid.NewGuid().ToString() };

            _viewLifecycleHandlerMock
                .Setup(x => x.CreateAsync(viewEntity, It.IsAny<ViewRevisionEntity?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Creation failed"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _sut.CreateAsync(viewEntity, categoryKeys));
            _viewLifecycleHandlerMock.Verify(x => x.CreateAsync(viewEntity, It.IsAny<ViewRevisionEntity?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}