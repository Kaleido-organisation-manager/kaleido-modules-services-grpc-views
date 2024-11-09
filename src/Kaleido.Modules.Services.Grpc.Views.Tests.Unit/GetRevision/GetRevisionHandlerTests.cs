using AutoMapper;
using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Common.Mappers;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Validators;
using Kaleido.Modules.Services.Grpc.Views.Delete;
using Kaleido.Modules.Services.Grpc.Views.GetRevision;
using Moq;
using Moq.AutoMock;
using Xunit;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Unit.GetRevision
{
    public class GetRevisionHandlerTests
    {
        private readonly AutoMocker _mocker;
        private readonly Mock<IGetRevisionManager> _getRevisionManagerMock;
        private readonly GetRevisionHandler _sut;

        public GetRevisionHandlerTests()
        {
            _mocker = new AutoMocker();
            _getRevisionManagerMock = _mocker.GetMock<IGetRevisionManager>();
            var mapper = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ViewMappingProfile>();
            });
            _mocker.Use(mapper.CreateMapper());
            _mocker.Use(new KeyValidator());
            _sut = _mocker.CreateInstance<GetRevisionHandler>();
        }

        [Fact]
        public async Task HandleAsync_ValidRequest_ReturnsViewResponse()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var request = new GetViewRevisionRequest { Key = key, CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow) };
            var viewEntity = new ViewEntity { /* Initialize properties */ };
            var viewRevisionEntity = new ViewRevisionEntity { /* Initialize properties */ };
            var categoryViewLinkResults = new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>();

            var viewResult = new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
            {
                Entity = viewEntity,
                Revision = viewRevisionEntity
            };

            _getRevisionManagerMock.Setup(m => m.GetViewRevision(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((viewResult, categoryViewLinkResults));

            // Act
            var response = await _sut.HandleAsync(request, CancellationToken.None);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(viewResult.Key.ToString(), response.Key);
            Assert.Equal(categoryViewLinkResults.Count, response.View.Categories.Count);
        }

        [Fact]
        public async Task HandleAsync_InvalidKey_ThrowsRpcException()
        {
            // Arrange
            var request = new GetViewRevisionRequest { Key = "invalid-guid", CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow) };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() => _sut.HandleAsync(request, CancellationToken.None));
            Assert.Equal(StatusCode.InvalidArgument, exception.Status.StatusCode);
        }

        [Fact]
        public async Task HandleAsync_ViewNotFound_ThrowsRpcException()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var request = new GetViewRevisionRequest { Key = key, CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow) };

            _getRevisionManagerMock.Setup(m => m.GetViewRevision(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((null, Enumerable.Empty<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>()));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() => _sut.HandleAsync(request, CancellationToken.None));
            Assert.Equal(StatusCode.NotFound, exception.Status.StatusCode);
        }

        [Fact]
        public async Task HandleAsync_ExceptionThrown_ThrowsRpcException()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var request = new GetViewRevisionRequest { Key = key, CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow) };

            _getRevisionManagerMock.Setup(m => m.GetViewRevision(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Some error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() => _sut.HandleAsync(request, CancellationToken.None));
            Assert.Equal(StatusCode.Internal, exception.Status.StatusCode);
        }
    }
}
