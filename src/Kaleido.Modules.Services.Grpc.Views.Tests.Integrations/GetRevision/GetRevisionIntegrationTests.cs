using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Kaleido.Grpc.Categories;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Tests.Integrations.Fixtures;
using Xunit;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Integrations.GetRevision
{
    public class GetRevisionIntegrationTests : IClassFixture<InfrastructureFixture>
    {
        private readonly InfrastructureFixture _fixture;

        public GetRevisionIntegrationTests(InfrastructureFixture fixture)
        {
            _fixture = fixture;
            _fixture.ClearDatabase().Wait();
        }

        [Fact]
        public async Task GetRevision_WhenViewExists_ReturnsViewRevision()
        {
            // Arrange
            var createView = new View
            {
                Name = "Test View",
                Categories = { }
            };

            var categories = new List<Category>()
            {
                new Category { Name = "Category 1" },
                new Category { Name = "Category 2" }
            };

            var categoryKeys = new List<string>();
            foreach (var category in categories)
            {
                var createdCategory = await _fixture.CategoriesClient.CreateCategoryAsync(category);
                categoryKeys.Add(createdCategory.Key);
            }

            createView.Categories.AddRange(categoryKeys);

            var createdView = await _fixture.Client.CreateViewAsync(createView);

            var revisionRequest = new ViewActionRequest
            {
                Key = createdView.Key,
                View = { Name = "Updated View" }
            };
            await _fixture.Client.UpdateViewAsync(revisionRequest);

            // Act
            var request = new GetViewRevisionRequest { Key = createdView.Key, CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow) };
            var response = await _fixture.Client.GetViewRevisionAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.Equal("Updated View", response.View.Name);
            Assert.Equal(createdView.Key, response.Key);
        }

        [Fact]
        public async Task GetRevision_WhenViewDoesNotExist_ThrowsNotFoundException()
        {
            // Arrange
            var request = new GetViewRevisionRequest { Key = Guid.NewGuid().ToString(), CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow) };

            // Act
            var act = async () => await _fixture.Client.GetViewRevisionAsync(request);

            // Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() => act());
            Assert.Equal(StatusCode.NotFound, exception.Status.StatusCode);
        }

        [Fact]
        public async Task GetRevision_WithInvalidKey_ThrowsInvalidArgumentException()
        {
            // Arrange
            var request = new GetViewRevisionRequest { Key = "invalid-guid", CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow) };

            // Act
            var act = async () => await _fixture.Client.GetViewRevisionAsync(request);

            // Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() => act());
            Assert.Equal(StatusCode.InvalidArgument, exception.Status.StatusCode);
        }

        [Fact]
        public async Task GetRevision_WhenRevisionDoesNotExist_ThrowsNotFoundException()
        {
            // Arrange
            var createView = new View
            {
                Name = "Test View",
                Categories = { }
            };

            var categories = new List<Category>()
            {
                new Category { Name = "Category 1" },
                new Category { Name = "Category 2" }
            };

            var categoryKeys = new List<string>();
            foreach (var category in categories)
            {
                var createdCategory = await _fixture.CategoriesClient.CreateCategoryAsync(category);
                categoryKeys.Add(createdCategory.Key);
            }

            createView.Categories.AddRange(categoryKeys);

            var createdView = await _fixture.Client.CreateViewAsync(createView);

            var request = new GetViewRevisionRequest { Key = createdView.Key, CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-1)) }; // Assuming no revisions exist for this date

            // Act
            var act = async () => await _fixture.Client.GetViewRevisionAsync(request);

            // Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() => act());
            Assert.Equal(StatusCode.NotFound, exception.Status.StatusCode);
        }
    }
}
