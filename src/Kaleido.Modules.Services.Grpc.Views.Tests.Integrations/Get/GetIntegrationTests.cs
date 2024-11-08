using Grpc.Core;
using Kaleido.Grpc.Categories;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Tests.Integrations.Fixtures;
using Xunit;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Integrations.Get
{
    public class GetIntegrationTests : IClassFixture<InfrastructureFixture>
    {
        private readonly InfrastructureFixture _fixture;

        public GetIntegrationTests(InfrastructureFixture fixture)
        {
            _fixture = fixture;
            _fixture.ClearDatabase().Wait();
        }

        [Fact]
        public async Task Get_WhenViewExists_ReturnsView()
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

            // Act
            var request = new ViewRequest { Key = createdView.Key };
            var response = await _fixture.Client.GetViewAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.Equal("Test View", response.View.Name);
            Assert.Equal(createdView.Key, response.Key);
        }

        [Fact]
        public async Task Get_WhenViewDoesNotExist_ThrowsNotFoundException()
        {
            // Arrange
            var request = new ViewRequest { Key = Guid.NewGuid().ToString() };

            // Act
            var act = async () => await _fixture.Client.GetViewAsync(request);

            // Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() => act());
            Assert.Equal(StatusCode.NotFound, exception.Status.StatusCode);
        }

        [Fact]
        public async Task Get_WithInvalidKey_ThrowsInvalidArgumentException()
        {
            // Arrange
            var request = new ViewRequest { Key = "invalid-guid" };

            // Act
            var act = async () => await _fixture.Client.GetViewAsync(request);

            // Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() => act());
            Assert.Equal(StatusCode.InvalidArgument, exception.Status.StatusCode);
        }
    }
}
