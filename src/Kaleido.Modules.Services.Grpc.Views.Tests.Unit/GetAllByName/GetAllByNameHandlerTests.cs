using AutoMapper;
using FluentValidation;
using Grpc.Core;
using Kaleido.Common.Services.Grpc.Constants;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Common.Mappers;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Validators;
using Kaleido.Modules.Services.Grpc.Views.GetAllByName;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.AutoMock;
using Xunit;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Unit.GetAllByName
{
    public class GetAllByNameHandlerTests
    {
        private readonly AutoMocker _mocker;
        private readonly Mock<IGetAllByNameManager> _getAllByNameManagerMock;
        private readonly GetAllByNameHandler _sut;

        public GetAllByNameHandlerTests()
        {
            _mocker = new AutoMocker();
            _getAllByNameManagerMock = _mocker.GetMock<IGetAllByNameManager>();
            var mapper = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ViewMappingProfile>();
            });
            _mocker.Use(mapper.CreateMapper());
            _mocker.Use(new NameValidator());
            _mocker.Use(NullLogger<GetAllByNameHandler>.Instance);
            _sut = _mocker.CreateInstance<GetAllByNameHandler>();
        }

        [Fact]
        public async Task HandleAsync_ValidRequest_ReturnsViewListResponse()
        {
            // Arrange
            var request = new GetAllViewsByNameRequest { Name = "Test View" };
            var expectedViews = new List<EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>>
            {
                new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
                {
                    Entity = new ViewEntity { Name = "Test View" },
                    Revision = new ViewRevisionEntity { /* Initialize properties */ }
                }
            };

            var expectedCategoryViewLinkResults = new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>
            {
                new EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>
                {
                    Entity = new CategoryViewLinkEntity { ViewKey = Guid.NewGuid() },
                    Revision = new CategoryViewLinkRevisionEntity { Action = RevisionAction.Created }
                }
            };

            var managerResult = new List<(EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>, IEnumerable<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>)>();
            foreach (var view in expectedViews)
            {
                managerResult.Add((view, expectedCategoryViewLinkResults));
            }

            _getAllByNameManagerMock.Setup(m => m.GetAllAsync(request.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(managerResult);

            // Act
            var result = await _sut.HandleAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedViews.Count, result.Views.Count);
            _getAllByNameManagerMock.Verify(m => m.GetAllAsync(request.Name, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_NoViewsFound_ReturnsEmptyResponse()
        {
            // Arrange
            var request = new GetAllViewsByNameRequest { Name = "Nonexistent View" };

            var managerResult = new List<(EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>, IEnumerable<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>)>();

            _getAllByNameManagerMock.Setup(m => m.GetAllAsync(request.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(managerResult);

            // Act
            var result = await _sut.HandleAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Views);
            _getAllByNameManagerMock.Verify(m => m.GetAllAsync(request.Name, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_UnexpectedException_ThrowsRpcException()
        {
            // Arrange
            var request = new GetAllViewsByNameRequest { Name = "Test View" };
            _getAllByNameManagerMock.Setup(m => m.GetAllAsync(request.Name, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() => _sut.HandleAsync(request));
            Assert.Equal(StatusCode.Internal, exception.Status.StatusCode);
        }
    }
}
