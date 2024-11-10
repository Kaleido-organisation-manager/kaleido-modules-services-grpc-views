using System.Linq.Expressions;
using Kaleido.Common.Services.Grpc.Constants;
using Kaleido.Common.Services.Grpc.Exceptions;
using Kaleido.Common.Services.Grpc.Handlers.Interfaces;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Kaleido.Modules.Services.Grpc.Views.Update;
using Moq;
using Moq.AutoMock;
using Xunit;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Unit.Update
{
    public class UpdateManagerTests
    {
        private readonly AutoMocker _mocker;
        private readonly Mock<IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity>> _viewLifecycleHandlerMock;
        private readonly Mock<IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>> _categoryViewLinkLifecycleHandlerMock;
        private readonly UpdateManager _sut;

        public UpdateManagerTests()
        {
            _mocker = new AutoMocker();
            _viewLifecycleHandlerMock = _mocker.GetMock<IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity>>();
            _categoryViewLinkLifecycleHandlerMock = _mocker.GetMock<IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>();
            _sut = new UpdateManager(_viewLifecycleHandlerMock.Object, _categoryViewLinkLifecycleHandlerMock.Object);
        }

        [Fact]
        public async Task UpdateAsync_ValidRequest_UpdatesViewAndLinks()
        {
            // Arrange
            var key = Guid.NewGuid();
            var viewEntity = new ViewEntity { Name = "Updated View" };
            var categoryKeys = new List<string> { Guid.NewGuid().ToString() };
            var viewResult = new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
            {
                Entity = viewEntity,
                Revision = new ViewRevisionEntity { Key = Guid.NewGuid() }
            };

            _viewLifecycleHandlerMock
                .Setup(x => x.GetAsync(key, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(viewResult);

            var categoryViewLinkResults = new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>();
            foreach (var categoryKey in categoryKeys)
            {
                var categoryViewLinkEntity = new CategoryViewLinkEntity { CategoryKey = Guid.Parse(categoryKey), ViewKey = key };
                var categoryViewLinkResult = new EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>
                {
                    Entity = categoryViewLinkEntity,
                    Revision = new CategoryViewLinkRevisionEntity { Key = Guid.NewGuid() }
                };
                categoryViewLinkResults.Add(categoryViewLinkResult);
            }

            _categoryViewLinkLifecycleHandlerMock
                    .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<CategoryViewLinkEntity, bool>>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(categoryViewLinkResults);

            _viewLifecycleHandlerMock
                .Setup(x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<ViewEntity>(), It.IsAny<ViewRevisionEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid key, ViewEntity entity, ViewRevisionEntity revision, CancellationToken _) =>
                new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
                {
                    Entity = entity,
                    Revision = revision ?? new ViewRevisionEntity()
                });

            // Act
            var result = await _sut.UpdateAsync(key, viewEntity, categoryKeys);

            // Assert
            Assert.NotNull(result.Item1);
            Assert.Equal(viewEntity, result.Item1.Entity);
            Assert.Equal(categoryViewLinkResults, result.Item2);
            _viewLifecycleHandlerMock.Verify(x => x.UpdateAsync(key, It.IsAny<ViewEntity>(), It.IsAny<ViewRevisionEntity?>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ViewDoesNotExist_ThrowsEntityNotFoundException()
        {
            // Arrange
            var key = Guid.NewGuid();
            var viewEntity = new ViewEntity { Name = "Updated View" };
            var categoryKeys = new List<string> { Guid.NewGuid().ToString() };

            _viewLifecycleHandlerMock
                .Setup(x => x.GetAsync(key, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>?)null);

            // Act & Assert
            await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.UpdateAsync(key, viewEntity, categoryKeys));
        }

        [Fact]
        public async Task UpdateAsync_NotModified_ReturnsView()
        {
            // Arrange
            var key = Guid.NewGuid();
            var viewEntity = new ViewEntity { Name = "Updated View" };
            var categoryKeys = new List<string> { Guid.NewGuid().ToString() };
            var viewResult = new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
            {
                Entity = viewEntity,
                Revision = new ViewRevisionEntity { Key = Guid.NewGuid() }
            };

            _viewLifecycleHandlerMock
                .Setup(x => x.GetAsync(key, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(viewResult);

            _viewLifecycleHandlerMock
                .Setup(x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<ViewEntity>(), It.IsAny<ViewRevisionEntity>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NotModifiedException("Not modified"));

            // Act
            var result = await _sut.UpdateAsync(key, viewEntity, categoryKeys);

            // Assert
            Assert.NotNull(result.Item1);
            Assert.Equal(viewResult.Entity, result.Item1.Entity);
            _viewLifecycleHandlerMock.Verify(x => x.UpdateAsync(key, It.IsAny<ViewEntity>(), It.IsAny<ViewRevisionEntity?>(), It.IsAny<CancellationToken>()), Times.Once);
            _viewLifecycleHandlerMock.Verify(x => x.GetAsync(key, It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
            _categoryViewLinkLifecycleHandlerMock.Verify(x => x.FindAllAsync(It.IsAny<Expression<Func<CategoryViewLinkEntity, bool>>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_RemoveCategories_DeletesCategories()
        {
            // Arrange
            var key = Guid.NewGuid();
            var viewEntity = new ViewEntity { Name = "Updated View" };
            var categoryKeys = new List<string> { Guid.NewGuid().ToString() };
            var viewResult = new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
            {
                Entity = viewEntity,
                Revision = new ViewRevisionEntity { Key = Guid.NewGuid() }
            };

            _viewLifecycleHandlerMock
                .Setup(x => x.GetAsync(key, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(viewResult);

            _viewLifecycleHandlerMock
                .Setup(x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<ViewEntity>(), It.IsAny<ViewRevisionEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(viewResult);

            var categoryViewLinkResults = new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>()
            {
                new EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>
                {
                    Entity = new CategoryViewLinkEntity { CategoryKey = Guid.NewGuid(), ViewKey = key },
                    Revision = new CategoryViewLinkRevisionEntity { Key = Guid.NewGuid() }
                }
            };

            _categoryViewLinkLifecycleHandlerMock
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<CategoryViewLinkEntity, bool>>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(categoryViewLinkResults);

            // Act
            var result = await _sut.UpdateAsync(key, viewEntity, categoryKeys);

            // Assert
            Assert.NotNull(result.Item1);
            Assert.Equal(viewResult.Entity, result.Item1.Entity);
            _categoryViewLinkLifecycleHandlerMock.Verify(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CategoryViewLinkRevisionEntity?>(), It.IsAny<CancellationToken>()), Times.Once);
            _categoryViewLinkLifecycleHandlerMock.Verify(x => x.CreateAsync(It.IsAny<CategoryViewLinkEntity>(), It.IsAny<CategoryViewLinkRevisionEntity?>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_AddDeletedCategories_RestoresCategories()
        {
            // Arrange
            var key = Guid.NewGuid();
            var viewEntity = new ViewEntity { Name = "Updated View" };
            var categoryKey = Guid.NewGuid().ToString();
            var categoryKeys = new List<string> { categoryKey };
            var viewResult = new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
            {
                Entity = viewEntity,
                Revision = new ViewRevisionEntity { Key = Guid.NewGuid() }
            };

            _viewLifecycleHandlerMock
                .Setup(x => x.GetAsync(key, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(viewResult);

            _viewLifecycleHandlerMock
                .Setup(x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<ViewEntity>(), It.IsAny<ViewRevisionEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(viewResult);

            var categoryViewLinkResults = new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>()
            {
                new EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>
                {
                    Entity = new CategoryViewLinkEntity { CategoryKey = Guid.Parse(categoryKey), ViewKey = key },
                    Revision = new CategoryViewLinkRevisionEntity { Key = Guid.NewGuid(), Action = RevisionAction.Deleted }
                }
            };

            _categoryViewLinkLifecycleHandlerMock
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<CategoryViewLinkEntity, bool>>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(categoryViewLinkResults);

            _categoryViewLinkLifecycleHandlerMock
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<CategoryViewLinkEntity, bool>>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(categoryViewLinkResults);

            // Act
            var result = await _sut.UpdateAsync(key, viewEntity, categoryKeys);

            // Assert
            Assert.NotNull(result.Item1);
            Assert.Equal(viewResult.Entity, result.Item1.Entity);
            _categoryViewLinkLifecycleHandlerMock.Verify(x => x.FindAllAsync(It.IsAny<Expression<Func<CategoryViewLinkEntity, bool>>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
            _categoryViewLinkLifecycleHandlerMock.Verify(x => x.RestoreAsync(It.IsAny<Guid>(), It.IsAny<CategoryViewLinkRevisionEntity?>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_AddNewCategories_CreatesCategories()
        {
            // Arrange
            var key = Guid.NewGuid();
            var viewEntity = new ViewEntity { Name = "Updated View" };
            var categoryKey = Guid.NewGuid().ToString();
            var categoryKeys = new List<string> { categoryKey, Guid.NewGuid().ToString() };
            var viewResult = new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
            {
                Entity = viewEntity,
                Revision = new ViewRevisionEntity { Key = Guid.NewGuid() }
            };

            _viewLifecycleHandlerMock
                .Setup(x => x.GetAsync(key, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(viewResult);

            _viewLifecycleHandlerMock
                .Setup(x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<ViewEntity>(), It.IsAny<ViewRevisionEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(viewResult);

            var categoryViewLinkResults = new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>()
            {
                new EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>
                {
                    Entity = new CategoryViewLinkEntity { CategoryKey = Guid.Parse(categoryKey), ViewKey = key },
                    Revision = new CategoryViewLinkRevisionEntity { Key = Guid.NewGuid() }
                }
            };

            _categoryViewLinkLifecycleHandlerMock
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<CategoryViewLinkEntity, bool>>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(categoryViewLinkResults);

            // Act
            var result = await _sut.UpdateAsync(key, viewEntity, categoryKeys);

            // Assert
            Assert.NotNull(result.Item1);
            Assert.Equal(viewResult.Entity, result.Item1.Entity);
            _categoryViewLinkLifecycleHandlerMock.Verify(x => x.CreateAsync(It.IsAny<CategoryViewLinkEntity>(), It.IsAny<CategoryViewLinkRevisionEntity?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
