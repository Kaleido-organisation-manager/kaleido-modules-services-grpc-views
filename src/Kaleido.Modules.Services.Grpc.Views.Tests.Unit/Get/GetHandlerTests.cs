using AutoMapper;
using FluentValidation;
using Grpc.Core;
using Kaleido.Common.Services.Grpc.Exceptions;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Common.Mappers;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Validators;
using Kaleido.Modules.Services.Grpc.Views.Get;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.AutoMock;
using Xunit;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Unit.Get
{
    public class GetHandlerTests
    {
        private readonly AutoMocker _mocker;
        private readonly Mock<IGetManager> _getManagerMock;
        private readonly GetHandler _sut;

        public GetHandlerTests()
        {
            _mocker = new AutoMocker();
            _getManagerMock = _mocker.GetMock<IGetManager>();
            var mapper = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ViewMappingProfile>();
            });
            _mocker.Use(mapper.CreateMapper());
            _mocker.Use(NullLogger<GetHandler>.Instance);
            _mocker.Use(new KeyValidator());
            _sut = _mocker.CreateInstance<GetHandler>();
        }

        [Fact]
        public async Task HandleAsync_ValidRequest_ReturnsViewResponse()
        {
            // Arrange
            var key = Guid.NewGuid();
            var request = new ViewRequest { Key = key.ToString() };
            var viewEntity = new ViewEntity { /* Initialize properties */ };
            var viewRevisionEntity = new ViewRevisionEntity { /* Initialize properties */ };
            var categoryViewLinkResults = new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>()
            {
                new EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>
                {
                    Entity = new CategoryViewLinkEntity(),
                    Revision = new CategoryViewLinkRevisionEntity()
                }
            };

            var viewResult = new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
            {
                Entity = viewEntity,
                Revision = viewRevisionEntity
            };

            _getManagerMock.Setup(m => m.GetAsync(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync((viewResult, categoryViewLinkResults));

            // Act
            var result = await _sut.HandleAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(viewResult.Key.ToString(), result.Key);
            Assert.Equal(categoryViewLinkResults.Count, result.View.Categories.Count);
            _getManagerMock.Verify(m => m.GetAsync(key, It.IsAny<CancellationToken>()), Times.Once);
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
        public async Task HandleAsync_ViewNotFound_ThrowsRpcException()
        {
            // Arrange
            var key = Guid.NewGuid();
            var request = new ViewRequest { Key = key.ToString() };

            _getManagerMock.Setup(m => m.GetAsync(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync((null, Enumerable.Empty<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>()));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() => _sut.HandleAsync(request));
            Assert.Equal(StatusCode.NotFound, exception.Status.StatusCode);
        }

        [Fact]
        public async Task HandleAsync_RevisionNotFound_ThrowsRpcException()
        {
            // Arrange
            var key = Guid.NewGuid();
            var request = new ViewRequest { Key = key.ToString() };

            _getManagerMock.Setup(m => m.GetAsync(key, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RevisionNotFoundException("Revision not found"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() => _sut.HandleAsync(request));
            Assert.Equal(StatusCode.NotFound, exception.Status.StatusCode);
        }

        [Fact]
        public async Task HandleAsync_UnexpectedException_ThrowsRpcException()
        {
            // Arrange
            var key = Guid.NewGuid();
            var request = new ViewRequest { Key = key.ToString() };

            _getManagerMock.Setup(m => m.GetAsync(key, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() => _sut.HandleAsync(request));
            Assert.Equal(StatusCode.Internal, exception.Status.StatusCode);
        }
    }
}
