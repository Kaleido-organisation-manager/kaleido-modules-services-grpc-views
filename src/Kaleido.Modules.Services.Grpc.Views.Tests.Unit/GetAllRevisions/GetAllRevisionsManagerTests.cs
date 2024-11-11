using System.Linq.Expressions;
using AutoMapper;
using Kaleido.Common.Services.Grpc.Handlers.Interfaces;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Mappers;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Kaleido.Modules.Services.Grpc.Views.GetAllRevisions;
using Moq;
using Moq.AutoMock;
using Xunit;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Unit.GetAllRevisions
{
    public class GetAllRevisionsManagerTests
    {
        private readonly AutoMocker _mocker;
        private readonly Mock<IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity>> _viewLifecycleHandlerMock;
        private readonly Mock<IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>> _categoryViewLinkLifecycleHandlerMock;
        private readonly GetAllRevisionsManager _sut;

        public GetAllRevisionsManagerTests()
        {
            _mocker = new AutoMocker();
            _viewLifecycleHandlerMock = _mocker.GetMock<IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity>>();
            _categoryViewLinkLifecycleHandlerMock = _mocker.GetMock<IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>();
            var mapper = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ViewMappingProfile>();
            });
            _mocker.Use(mapper.CreateMapper());
            _sut = _mocker.CreateInstance<GetAllRevisionsManager>();
        }

        [Fact]
        public async Task GetAllRevisionAsync_ReturnsRevisions_WhenDataExists()
        {
            // Arrange
            var key = Guid.NewGuid();
            var viewRevisions = new List<EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>>
            {
                new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
                {
                    Entity = new ViewEntity(),
                    Revision = new ViewRevisionEntity { CreatedAt = DateTime.UtcNow.AddDays(-1) }
                }
            };

            var categoryViewLinkRevisions = new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>
            {
                new EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>
                {
                    Entity = new CategoryViewLinkEntity(),
                    Revision = new CategoryViewLinkRevisionEntity { CreatedAt = viewRevisions.First().Revision.CreatedAt }
                }
            };

            _viewLifecycleHandlerMock
                .Setup(x => x.GetAllAsync(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(viewRevisions);

            _categoryViewLinkLifecycleHandlerMock
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<CategoryViewLinkEntity, bool>>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(categoryViewLinkRevisions);

            // Act
            var result = await _sut.GetAllRevisionAsync(key, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(viewRevisions.First().Revision.CreatedAt, result.First().Item1.Revision.CreatedAt);
            Assert.Equal(categoryViewLinkRevisions.First().Revision.CreatedAt, result.First().Item2.First().Revision.CreatedAt);
        }

        [Fact]
        public async Task GetAllRevisionAsync_ReturnsEmpty_WhenNoDataExists()
        {
            // Arrange
            var key = Guid.NewGuid();
            _viewLifecycleHandlerMock
                .Setup(x => x.GetAllAsync(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>>());

            _categoryViewLinkLifecycleHandlerMock
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<CategoryViewLinkEntity, bool>>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>());

            // Act
            var result = await _sut.GetAllRevisionAsync(key, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllRevisionAsync_ThrowsException_WhenViewLifecycleHandlerFails()
        {
            // Arrange
            var key = Guid.NewGuid();
            _viewLifecycleHandlerMock
                .Setup(x => x.GetAllAsync(key, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("View lifecycle handler error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _sut.GetAllRevisionAsync(key, CancellationToken.None));
        }

        [Fact]
        public async Task GetAllRevisionAsync_ThrowsException_WhenCategoryViewLinkLifecycleHandlerFails()
        {
            // Arrange
            var key = Guid.NewGuid();
            var viewRevisions = new List<EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>>
            {
                new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
                {
                    Entity = new ViewEntity(),
                    Revision = new ViewRevisionEntity { CreatedAt = DateTime.UtcNow.AddDays(-1) }
                }
            };

            _viewLifecycleHandlerMock
                .Setup(x => x.GetAllAsync(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(viewRevisions);

            _categoryViewLinkLifecycleHandlerMock
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<CategoryViewLinkEntity, bool>>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Category view link lifecycle handler error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _sut.GetAllRevisionAsync(key, CancellationToken.None));
        }

        [Fact]
        public async Task GetAllRevisionAsync_ReturnsRevisions_WhenMultipleRevisionsExist()
        {
            // Arrange
            var key = Guid.NewGuid();
            var viewRevisions = new List<EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>>
            {
                new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
                {
                    Entity = new ViewEntity(),
                    Revision = new ViewRevisionEntity { Key = key, CreatedAt = DateTime.UtcNow.AddDays(-1) }
                },
                new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
                {
                    Entity = new ViewEntity(),
                    Revision = new ViewRevisionEntity { Key = key, CreatedAt = DateTime.UtcNow.AddDays(-2) }
                }
            };

            var viewCategoryLinkRevisions = new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>();

            foreach (var viewRevision in viewRevisions)
            {
                viewCategoryLinkRevisions.AddRange(
                    [
                        new EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>
                        {
                            Entity = new CategoryViewLinkEntity(),
                            Revision = new CategoryViewLinkRevisionEntity { Key = Guid.NewGuid(), CreatedAt = viewRevision.Revision.CreatedAt }
                        },
                        new EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>
                        {
                            Entity = new CategoryViewLinkEntity(),
                            Revision = new CategoryViewLinkRevisionEntity { Key = Guid.NewGuid(), CreatedAt = viewRevision.Revision.CreatedAt }
                        }
                    ]
                );
            }

            _viewLifecycleHandlerMock
                .Setup(x => x.GetAllAsync(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(viewRevisions);

            _categoryViewLinkLifecycleHandlerMock
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<CategoryViewLinkEntity, bool>>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(viewCategoryLinkRevisions);

            // Act
            var result = await _sut.GetAllRevisionAsync(key, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(viewRevisions.Count(), result.Count());
        }

        [Fact]
        public async Task GetAllRevisionAsync_ReturnsRevisions_WhenMultipleCategoryViewLinkRevisionsExist()
        {
            // Arrange
            var key = Guid.NewGuid();
            var viewRevisions = new List<EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>>()
            {
                new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
                {
                    Entity = new ViewEntity(),
                    Revision = new ViewRevisionEntity { Key = key, CreatedAt = DateTime.UtcNow.AddDays(-2) }
                }
            };

            var viewCategoryLinkRevisions = new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>()
            {
                new EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>
                {
                    Entity = new CategoryViewLinkEntity(),
                    Revision = new CategoryViewLinkRevisionEntity { Key = Guid.NewGuid(), CreatedAt = viewRevisions.First().Revision.CreatedAt }
                },
                new EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>
                {
                    Entity = new CategoryViewLinkEntity(),
                    Revision = new CategoryViewLinkRevisionEntity { Key = Guid.NewGuid(), CreatedAt = DateTime.UtcNow.AddDays(-1) }
                }
            };

            _viewLifecycleHandlerMock
                .Setup(x => x.GetAllAsync(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(viewRevisions);

            _categoryViewLinkLifecycleHandlerMock
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<CategoryViewLinkEntity, bool>>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(viewCategoryLinkRevisions);

            // Act
            var result = await _sut.GetAllRevisionAsync(key, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(viewCategoryLinkRevisions.Count(), result.Count());
        }
    }
}