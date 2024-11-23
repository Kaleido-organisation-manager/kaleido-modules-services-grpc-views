using AutoMapper;
using FluentValidation;
using Grpc.Core;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Common.Mappers;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Kaleido.Modules.Services.Grpc.Views.GetAll;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.AutoMock;
using Xunit;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Unit.GetAll
{
    public class GetAllHandlerTests
    {
        private readonly AutoMocker _mocker;
        private readonly Mock<IGetAllManager> _getAllManagerMock;
        private readonly GetAllHandler _sut;

        public GetAllHandlerTests()
        {
            _mocker = new AutoMocker();
            _getAllManagerMock = _mocker.GetMock<IGetAllManager>();
            var mapper = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ViewMappingProfile>();
            });
            _mocker.Use(mapper.CreateMapper());
            _mocker.Use(NullLogger<GetAllHandler>.Instance);
            _sut = _mocker.CreateInstance<GetAllHandler>();
        }

        [Fact]
        public async Task HandleAsync_ValidRequest_ReturnsViewListResponse()
        {
            // Arrange
            var expectedViews = new List<EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>>
            {
                new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
                {
                    Entity = new ViewEntity { /* Initialize properties */ },
                    Revision = new ViewRevisionEntity { /* Initialize properties */ }
                }
            };

            var expectedCategoryViewLinks = new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>
            {
                new EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>
                {
                    Entity = new CategoryViewLinkEntity { /* Initialize properties */ },
                    Revision = new CategoryViewLinkRevisionEntity { /* Initialize properties */ }
                }
            };

            var managerResult = new List<ManagerResponse>();
            foreach (var view in expectedViews)
            {
                managerResult.Add(new ManagerResponse(view, expectedCategoryViewLinks));
            }

            _getAllManagerMock.Setup(m => m.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(managerResult);

            // Act
            var result = await _sut.HandleAsync(new EmptyRequest());

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedViews.Count, result.Views.Count);
            _getAllManagerMock.Verify(m => m.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_UnexpectedException_ThrowsRpcException()
        {
            // Arrange
            _getAllManagerMock.Setup(m => m.GetAllAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() => _sut.HandleAsync(new EmptyRequest()));
            Assert.Equal(StatusCode.Internal, exception.Status.StatusCode);
        }
    }
}
