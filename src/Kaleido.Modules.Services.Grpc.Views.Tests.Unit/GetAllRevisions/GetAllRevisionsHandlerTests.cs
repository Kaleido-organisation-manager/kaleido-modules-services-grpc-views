using AutoMapper;
using Grpc.Core;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Common.Mappers;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Validators;
using Kaleido.Modules.Services.Grpc.Views.GetAllRevisions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.AutoMock;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Unit.GetAllRevisions
{
    public class GetAllRevisionsHandlerTests
    {
        private readonly AutoMocker _mocker;
        private readonly Mock<IGetAllRevisionsManager> _getAllRevisionsManagerMock;
        private readonly GetAllRevisionsHandler _sut;

        public GetAllRevisionsHandlerTests()
        {
            _mocker = new AutoMocker();
            _getAllRevisionsManagerMock = _mocker.GetMock<IGetAllRevisionsManager>();
            var mapper = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ViewMappingProfile>();
            });
            _mocker.Use(mapper.CreateMapper());
            _mocker.Use(new KeyValidator());
            _mocker.Use<ILogger<GetAllRevisionsHandler>>(NullLogger<GetAllRevisionsHandler>.Instance);
            _sut = _mocker.CreateInstance<GetAllRevisionsHandler>();
        }

        [Fact]
        public async Task HandleAsync_ValidRequest_ReturnsViewListResponse()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var request = new ViewRequest { Key = key };
            var viewEntity = new ViewEntity { /* Initialize properties */ };
            var viewRevisionEntity = new ViewRevisionEntity { /* Initialize properties */ };
            var categoryViewLinkResults = new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>();

            var result = new List<ManagerResponse>
            {
                new ManagerResponse(new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity> { Entity = viewEntity, Revision = viewRevisionEntity }, categoryViewLinkResults)
            };

            _getAllRevisionsManagerMock.Setup(m => m.GetAllRevisionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var viewWithCategoriesResult = new EntityLifeCycleResult<ViewWithCategories, BaseRevisionEntity>
            {
                Entity = new ViewWithCategories { Categories = categoryViewLinkResults },
                Revision = new BaseRevisionEntity()
            };

            // Act
            var response = await _sut.HandleAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.Single(response.Views);
            Assert.Equal(viewWithCategoriesResult.Entity.Categories.Count(), response.Views.First().View.Categories.Count());
        }

        [Fact]
        public async Task HandleAsync_InvalidKey_ThrowsRpcException()
        {
            // Arrange
            var request = new ViewRequest { Key = "invalid-guid" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() => _sut.HandleAsync(request));
            Assert.Equal(StatusCode.InvalidArgument, exception.Status.StatusCode);
        }

        [Fact]
        public async Task HandleAsync_ValidationFails_ThrowsRpcException()
        {
            // Arrange
            var request = new ViewRequest { Key = "null" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() => _sut.HandleAsync(request));
            Assert.Equal(StatusCode.InvalidArgument, exception.Status.StatusCode);
        }

        [Fact]
        public async Task HandleAsync_ExceptionThrown_ThrowsRpcException()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var request = new ViewRequest { Key = key };
            _getAllRevisionsManagerMock.Setup(m => m.GetAllRevisionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Some error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() => _sut.HandleAsync(request));
            Assert.Equal(StatusCode.Internal, exception.Status.StatusCode);
        }
    }
}
