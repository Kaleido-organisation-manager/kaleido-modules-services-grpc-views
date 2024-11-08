using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Kaleido.Common.Services.Grpc.Constants;
using Kaleido.Common.Services.Grpc.Handlers.Interfaces;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Kaleido.Modules.Services.Grpc.Views.GetAllByName;
using Moq;
using Moq.AutoMock;
using Xunit;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Unit.GetAllByName
{
    public class GetAllByNameManagerTests
    {
        private readonly AutoMocker _mocker;
        private readonly Mock<IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity>> _viewLifecycleHandlerMock;
        private readonly Mock<IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>> _categoryViewLinkLifecycleHandlerMock;
        private readonly GetAllByNameManager _sut;

        public GetAllByNameManagerTests()
        {
            _mocker = new AutoMocker();
            _viewLifecycleHandlerMock = _mocker.GetMock<IEntityLifecycleHandler<ViewEntity, ViewRevisionEntity>>();
            _categoryViewLinkLifecycleHandlerMock = _mocker.GetMock<IEntityLifecycleHandler<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>();
            _sut = new GetAllByNameManager(_viewLifecycleHandlerMock.Object, _categoryViewLinkLifecycleHandlerMock.Object);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsViewsAndLinks_WhenDataExists()
        {
            // Arrange
            var name = "Test View";
            var viewEntities = new List<EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>>
            {
                new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
                {
                    Entity = new ViewEntity { Name = name },
                    Revision = new ViewRevisionEntity { Action = RevisionAction.Created }
                }
            };

            var categoryViewLinkResults = new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>
            {
                new EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>
                {
                    Entity = new CategoryViewLinkEntity { ViewKey = Guid.NewGuid() },
                    Revision = new CategoryViewLinkRevisionEntity { Action = RevisionAction.Created }
                }
            };

            _viewLifecycleHandlerMock
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<ViewEntity, bool>>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(viewEntities);

            _categoryViewLinkLifecycleHandlerMock
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<CategoryViewLinkEntity, bool>>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(categoryViewLinkResults);

            // Act
            var result = await _sut.GetAllAsync(name, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(viewEntities.First(), result.First().Item1);
            Assert.Equal(categoryViewLinkResults, result.First().Item2);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsEmpty_WhenNoViewsExist()
        {
            // Arrange
            var name = "Nonexistent View";

            _viewLifecycleHandlerMock
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<ViewEntity, bool>>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>>());

            // Act
            var result = await _sut.GetAllAsync(name, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAsync_DoesNotIncludeDeletedViews()
        {
            // Arrange
            var name = "Deleted View";
            var viewEntities = new List<EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>>
            {
                new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
                {
                    Entity = new ViewEntity { Name = name },
                    Revision = new ViewRevisionEntity { Action = RevisionAction.Deleted }
                }
            };

            _viewLifecycleHandlerMock
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<ViewEntity, bool>>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(viewEntities);

            // Act
            var result = await _sut.GetAllAsync(name, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsOnlyActiveLinks_WhenLinksExist()
        {
            // Arrange
            var name = "View With Links";
            var viewEntities = new List<EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>>
            {
                new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
                {
                    Entity = new ViewEntity { Name = name },
                    Revision = new ViewRevisionEntity { Action = RevisionAction.Created }
                }
            };

            var categoryViewLinkResults = new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>
            {
                new EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>
                {
                    Entity = new CategoryViewLinkEntity { ViewKey = Guid.NewGuid() },
                    Revision = new CategoryViewLinkRevisionEntity { Action = RevisionAction.Deleted }
                },
                new EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>
                {
                    Entity = new CategoryViewLinkEntity { ViewKey = Guid.NewGuid() },
                    Revision = new CategoryViewLinkRevisionEntity { Action = RevisionAction.Created }
                }
            };

            _viewLifecycleHandlerMock
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<ViewEntity, bool>>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(viewEntities);

            _categoryViewLinkLifecycleHandlerMock
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<CategoryViewLinkEntity, bool>>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(categoryViewLinkResults);

            // Act
            var result = await _sut.GetAllAsync(name, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(viewEntities.First(), result.First().Item1);
            Assert.Single(result.First().Item2);
        }
    }
}
