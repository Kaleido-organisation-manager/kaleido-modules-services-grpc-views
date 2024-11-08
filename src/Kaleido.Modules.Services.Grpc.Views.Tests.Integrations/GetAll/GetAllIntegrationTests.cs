using Grpc.Core;
using Kaleido.Grpc.Categories;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Tests.Integrations.Fixtures;
using Xunit;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Integrations.GetAll
{
    public class GetAllIntegrationTests : IClassFixture<InfrastructureFixture>
    {
        private readonly InfrastructureFixture _fixture;

        public GetAllIntegrationTests(InfrastructureFixture fixture)
        {
            _fixture = fixture;
            _fixture.ClearDatabase().Wait();
        }

        [Fact]
        public async Task GetAll_WhenViewsAndLinksExist_ReturnsViewListResponse()
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
            var request = new Kaleido.Grpc.Views.EmptyRequest();
            var response = await _fixture.Client.GetAllViewsAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.NotEmpty(response.Views);
            Assert.Equal("Test View", response.Views.First().View.Name);
            Assert.Equal(createdView.Key, response.Views.First().Key);
            Assert.Equal(categories.Count, response.Views.First().View.Categories.Count);
        }

        [Fact]
        public async Task GetAll_WhenNoViewsExist_ReturnsEmptyResponse()
        {
            // Arrange
            var request = new Kaleido.Grpc.Views.EmptyRequest();

            // Act
            var response = await _fixture.Client.GetAllViewsAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.Empty(response.Views);
        }

        [Fact]
        public async Task GetAll_WhenViewIsDeleted_DoesNotIncludeIt()
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

            var deleteViewRequest = new ViewRequest { Key = createdView.Key };
            await _fixture.Client.DeleteViewAsync(deleteViewRequest);

            // Act
            var request = new Kaleido.Grpc.Views.EmptyRequest();
            var response = await _fixture.Client.GetAllViewsAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.Empty(response.Views);
        }
    }
}
