using Grpc.Core;
using Kaleido.Grpc.Categories;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Tests.Integrations.Fixtures;
using Xunit;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Integrations.GetAllRevisions
{
    public class GetAllRevisionsIntegrationTests : IClassFixture<InfrastructureFixture>
    {
        private readonly InfrastructureFixture _fixture;

        public GetAllRevisionsIntegrationTests(InfrastructureFixture fixture)
        {
            _fixture = fixture;
            _fixture.ClearDatabase().Wait();
        }

        [Fact]
        public async Task GetAllRevisions_WhenViewsAndLinksExist_ReturnsViewListResponse()
        {
            // Arrange
            var key = Guid.NewGuid();
            var createView = new View
            {
                Name = "Test View",
                Categories = { }
            };

            var categories = new List<Category>
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

            Console.WriteLine(createdView.Key);
            Console.WriteLine(categoryKeys.FirstOrDefault() ?? "No category key");

            // Create revisions for the view
            var updatedView = new View { Name = "Updated View", Categories = { categoryKeys.First() } };
            await _fixture.Client.UpdateViewAsync(new ViewActionRequest { Key = createdView.Key, View = updatedView });

            // Act
            var request = new ViewRequest { Key = createdView.Key };
            var response = await _fixture.Client.GetAllViewRevisionsAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.NotEmpty(response.Views);
            Assert.Equal("Test View", response.Views.First().View.Name);
            Assert.Equal(createdView.Key, response.Views.First().Key);
            Assert.Equal(2, response.Views.Count); // Expecting 2 revisions
        }

        [Fact]
        public async Task GetAllRevisions_WhenNoRevisionsExist_ReturnsEmptyResponse()
        {
            // Arrange
            var key = Guid.NewGuid();
            var createView = new View
            {
                Name = "Test View",
                Categories = { }
            };

            var createdView = await _fixture.Client.CreateViewAsync(createView);

            // Act
            var request = new ViewRequest { Key = createdView.Key };
            var response = await _fixture.Client.GetAllViewRevisionsAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.Empty(response.Views);
        }

        [Fact]
        public async Task GetAllRevisions_WhenViewIsDeleted_DoesNotIncludeIt()
        {
            // Arrange
            var key = Guid.NewGuid();
            var createView = new View
            {
                Name = "Test View to Delete",
                Categories = { }
            };

            var createdView = await _fixture.Client.CreateViewAsync(createView);
            await _fixture.Client.DeleteViewAsync(new ViewRequest { Key = createdView.Key });

            // Act
            var request = new ViewRequest { Key = createdView.Key };
            var response = await _fixture.Client.GetAllViewRevisionsAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.Empty(response.Views);
        }
    }
}
