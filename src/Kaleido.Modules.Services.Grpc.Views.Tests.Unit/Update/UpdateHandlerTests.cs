using AutoMapper;
using Grpc.Core;
using Kaleido.Common.Services.Grpc.Exceptions;
using Kaleido.Common.Services.Grpc.Models;
using Kaleido.Grpc.Categories;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Common.Mappers;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using Kaleido.Modules.Services.Grpc.Views.Common.Validators;
using Kaleido.Modules.Services.Grpc.Views.Update;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.AutoMock;
using static Kaleido.Grpc.Categories.GrpcCategories;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Unit.Update
{
    public class UpdateHandlerTests
    {
        private readonly AutoMocker _mocker;
        private readonly Mock<IUpdateManager> _updateManagerMock;
        private readonly Mock<GrpcCategoriesClient> _categoryClientMock;
        private readonly UpdateHandler _sut;

        public UpdateHandlerTests()
        {
            _mocker = new AutoMocker();
            _updateManagerMock = _mocker.GetMock<IUpdateManager>();
            var mapper = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ViewMappingProfile>();
            });
            _mocker.Use(mapper.CreateMapper());
            _mocker.Use(new KeyValidator());
            _mocker.Use<ILogger<ViewValidator>>(NullLogger<ViewValidator>.Instance);

            _categoryClientMock = new Mock<GrpcCategoriesClient>();
            _categoryClientMock.Setup(c => c.GetCategoryAsync(It.IsAny<CategoryRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                .Returns(new AsyncUnaryCall<CategoryResponse>(Task.FromResult(new CategoryResponse()), Task.FromResult(new Metadata()), null!, null!, null!));

            _mocker.Use(_categoryClientMock.Object);

            _mocker.Use(new ViewValidator(_categoryClientMock.Object, NullLogger<ViewValidator>.Instance));
            _sut = _mocker.CreateInstance<UpdateHandler>();
        }

        [Fact]
        public async Task HandleAsync_ValidRequest_UpdatesView()
        {
            // Arrange
            var request = new ViewActionRequest
            {
                Key = Guid.NewGuid().ToString(),
                View = new View { Name = "Updated View", Categories = { new List<string> { Guid.NewGuid().ToString() } } }
            };
            var viewEntity = new ViewEntity { Name = "Updated View" };
            var viewResult = new EntityLifeCycleResult<ViewEntity, ViewRevisionEntity>
            {
                Entity = viewEntity,
                Revision = new ViewRevisionEntity { Key = Guid.NewGuid() }
            };

            _updateManagerMock.Setup(m => m.UpdateAsync(It.IsAny<Guid>(), It.IsAny<ViewEntity>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((viewResult, new List<EntityLifeCycleResult<CategoryViewLinkEntity, CategoryViewLinkRevisionEntity>>()));

            // Act
            var response = await _sut.HandleAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(viewResult.Entity.Name, response.View.Name);
            _updateManagerMock.Verify(m => m.UpdateAsync(It.IsAny<Guid>(), It.IsAny<ViewEntity>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_InvalidKey_ThrowsRpcException()
        {
            // Arrange
            var request = new ViewActionRequest { Key = "invalid-guid" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() => _sut.HandleAsync(request));
            Assert.Equal(StatusCode.InvalidArgument, exception.Status.StatusCode);
        }

        [Fact]
        public async Task HandleAsync_ViewNotFound_ThrowsRpcException()
        {
            // Arrange
            var request = new ViewActionRequest
            {
                Key = Guid.NewGuid().ToString(),
                View = new View { Name = "Updated View", Categories = { new List<string> { Guid.NewGuid().ToString() } } }
            };
            var viewEntity = new ViewEntity { Name = "Updated View" };

            _updateManagerMock.Setup(m => m.UpdateAsync(It.IsAny<Guid>(), It.IsAny<ViewEntity>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new EntityNotFoundException("View not found"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() => _sut.HandleAsync(request));
            Assert.Equal(StatusCode.NotFound, exception.Status.StatusCode);
        }

        [Fact]
        public async Task HandleAsync_ValidationFails_ThrowsRpcException()
        {
            // Arrange
            var request = new ViewActionRequest { Key = Guid.NewGuid().ToString(), View = new View() };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() => _sut.HandleAsync(request));
            Assert.Equal(StatusCode.InvalidArgument, exception.Status.StatusCode);
        }
    }
}
