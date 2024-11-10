using Grpc.Core;
using Kaleido.Grpc.Categories;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Tests.Integrations.Fixtures;
using Xunit;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Integrations.Update
{
    public class UpdateIntegrationTests : IClassFixture<InfrastructureFixture>
    {
        private readonly InfrastructureFixture _fixture;

        public UpdateIntegrationTests(InfrastructureFixture fixture)
        {
            _fixture = fixture;
            _fixture.ClearDatabase().Wait();
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateView()
        {
            // Arrange
            var category = new Category { Name = "Test Category" };
            var createdCategory = await _fixture.CategoriesClient.CreateCategoryAsync(category);

            var createViewRequest = new View
            {
                Name = "Initial View",
                Categories = { createdCategory.Key }
            };
            var createdView = await _fixture.Client.CreateViewAsync(createViewRequest);

            var updateRequest = new ViewActionRequest
            {
                Key = createdView.Key,
                View = new View
                {
                    Name = "Updated View",
                    Categories = { createdCategory.Key }
                }
            };

            // Act
            var response = await _fixture.Client.UpdateViewAsync(updateRequest);

            // Assert
            Assert.NotNull(response);
            Assert.Equal("Updated View", response.View.Name);
            Assert.Contains(createdCategory.Key, response.View.Categories.Select(c => c.CategoryLink.Category));
        }

        [Fact]
        public async Task UpdateAsync_CategoryDoesNotExist_ShouldThrow()
        {
            // Arrange
            var request = new ViewActionRequest
            {
                Key = Guid.NewGuid().ToString(),
                View = new View
                {
                    Name = "Test View",
                    Categories = { Guid.NewGuid().ToString() }
                }
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(async () => await _fixture.Client.UpdateViewAsync(request));
            Assert.Equal(StatusCode.InvalidArgument, exception.Status.StatusCode);
        }

        [Fact]
        public async Task UpdateAsync_ViewDoesNotExist_ShouldThrow()
        {
            // Arrange
            var request = new ViewActionRequest
            {
                Key = Guid.NewGuid().ToString(),
                View = new View
                {
                    Name = "Nonexistent View",
                    Categories = { Guid.NewGuid().ToString() }
                }
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(async () => await _fixture.Client.UpdateViewAsync(request));
            Assert.Equal(StatusCode.NotFound, exception.Status.StatusCode);
        }

        [Fact]
        public async Task UpdateAsync_InvalidKey_ShouldThrow()
        {
            // Arrange
            var request = new ViewActionRequest
            {
                Key = "invalid-guid",
                View = new View
                {
                    Name = "Test View",
                    Categories = { }
                }
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(async () => await _fixture.Client.UpdateViewAsync(request));
            Assert.Equal(StatusCode.InvalidArgument, exception.Status.StatusCode);
        }
    }
}
