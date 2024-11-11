using Grpc.Core;
using Kaleido.Grpc.Categories;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Tests.Integrations.Fixtures;
using Microsoft.VisualBasic;
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
        public async Task UpdateAsync_CategoryIsDeleted_ShouldThrow()
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

            await _fixture.CategoriesClient.DeleteCategoryAsync(new CategoryRequest { Key = createdCategory.Key });

            var updateRequest = new ViewActionRequest
            {
                Key = createdView.Key,
                View = new View { Name = "Updated View", Categories = { createdCategory.Key } }
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(async () => await _fixture.Client.UpdateViewAsync(updateRequest));
            Assert.Equal(StatusCode.InvalidArgument, exception.Status.StatusCode);
        }

        [Fact]
        public async Task UpdateAsync_CategoryIsRemovedFromView_ShouldMarkAsDeleted()
        {
            // Arrange
            var categories = new List<Category>
            {
                new Category { Name = "Test Category 1" },
                new Category { Name = "Test Category 2" }
            };

            var categoryKeys = new List<string>();
            foreach (var category in categories)
            {
                var createdCategory = await _fixture.CategoriesClient.CreateCategoryAsync(category);
                categoryKeys.Add(createdCategory.Key);
            }

            var createViewRequest = new View
            {
                Name = "Initial View",
                Categories = { categoryKeys }
            };
            var createdView = await _fixture.Client.CreateViewAsync(createViewRequest);

            var updateRequest = new ViewActionRequest
            {
                Key = createdView.Key,
                View = new View { Name = "Updated View", Categories = { categoryKeys.First() } }
            };

            // Act
            var response = await _fixture.Client.UpdateViewAsync(updateRequest);

            // Assert
            Assert.NotNull(response);
            Assert.Equal("Updated View", response.View.Name);
            Assert.Contains(categoryKeys.Last(), response.View.Categories.Select(c => c.CategoryLink.Category));
            Assert.Equal("Deleted", response.View.Categories.FirstOrDefault(x => x.CategoryLink.Category == categoryKeys.Last())?.Revision.Action);
        }

        [Fact]
        public async Task UpdateAsync_WhenCategoryIsRestored_ShouldBeMarkedAsRestored()
        {
            // Arrange
            var categories = new List<Category>
            {
                new Category { Name = "Test Category 1" },
                new Category { Name = "Test Category 2" }
            };

            var categoryKeys = new List<string>();
            foreach (var category in categories)
            {
                var createdCategory = await _fixture.CategoriesClient.CreateCategoryAsync(category);
                categoryKeys.Add(createdCategory.Key);
            }

            var createViewRequest = new View
            {
                Name = "Initial View",
                Categories = { categoryKeys }
            };
            var createdView = await _fixture.Client.CreateViewAsync(createViewRequest);

            var updateRequest = new ViewActionRequest
            {
                Key = createdView.Key,
                View = new View { Name = "Updated View", Categories = { categoryKeys.First() } }
            };

            var updateRequest2 = new ViewActionRequest
            {
                Key = createdView.Key,
                View = new View { Name = "Updated View", Categories = { categoryKeys } }
            };

            await _fixture.Client.UpdateViewAsync(updateRequest);
            var response2 = await _fixture.Client.UpdateViewAsync(updateRequest2);

            // Assert
            Assert.NotNull(response2);
            Assert.Equal("Updated View", response2.View.Name);
            Assert.Contains(categoryKeys.Last(), response2.View.Categories.Select(c => c.CategoryLink.Category));
            Assert.Equal(2, response2.View.Categories.Count);
            Assert.Equal("Restored", response2.View.Categories.FirstOrDefault(x => x.CategoryLink.Category == categoryKeys.Last())?.Revision.Action);
        }

        [Fact]
        public async Task UpdateAsync_WhenCategoryIsDeleted_DoesNotDeleteItAgain()
        {
            // Arrange
            var categories = new List<Category>
            {
                new Category { Name = "Test Category 1" },
                new Category { Name = "Test Category 2" },
                new Category { Name = "Test Category 3" }
            };

            var categoryKeys = new List<string>();
            foreach (var category in categories)
            {
                var createdCategory = await _fixture.CategoriesClient.CreateCategoryAsync(category);
                categoryKeys.Add(createdCategory.Key);
            }

            var createViewRequest = new View
            {
                Name = "Initial View",
                Categories = { categoryKeys[0], categoryKeys[1] }
            };
            var createdView = await _fixture.Client.CreateViewAsync(createViewRequest);

            var updateRequest1 = new ViewActionRequest
            {
                Key = createdView.Key,
                View = new View { Name = "Updated View", Categories = { categoryKeys[0] } }
            };
            await _fixture.Client.UpdateViewAsync(updateRequest1);

            var updateRequest2 = new ViewActionRequest
            {
                Key = createdView.Key,
                View = new View { Name = "Updated View", Categories = { categoryKeys[0], categoryKeys[2] } }
            };

            var updateResponse2 = await _fixture.Client.UpdateViewAsync(updateRequest2);


            var response = await _fixture.Client.GetViewAsync(new ViewRequest { Key = createdView.Key });



            Assert.Equal(2, response.View.Categories.Count);
            Assert.Equal(updateResponse2.View.Categories.Count, response.View.Categories.Count);
            Assert.Contains(categoryKeys[2], response.View.Categories.Select(c => c.CategoryLink.Category));
            Assert.Contains(categoryKeys[0], response.View.Categories.Select(c => c.CategoryLink.Category));
            Assert.Contains(categoryKeys[2], updateResponse2.View.Categories.Select(c => c.CategoryLink.Category));
            Assert.Contains(categoryKeys[0], updateResponse2.View.Categories.Select(c => c.CategoryLink.Category));
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
            var category = new Category { Name = "Test Category" };
            var createdCategory = await _fixture.CategoriesClient.CreateCategoryAsync(category);

            var request = new ViewActionRequest
            {
                Key = Guid.NewGuid().ToString(),
                View = new View
                {
                    Name = "Nonexistent View",
                    Categories = { createdCategory.Key }
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
            var category = new Category { Name = "Test Category" };
            var createdCategory = await _fixture.CategoriesClient.CreateCategoryAsync(category);

            var request = new ViewActionRequest
            {
                Key = "invalid-guid",
                View = new View
                {
                    Name = "Test View",
                    Categories = { createdCategory.Key }
                }
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(async () => await _fixture.Client.UpdateViewAsync(request));
            Assert.Equal(StatusCode.InvalidArgument, exception.Status.StatusCode);
        }

        [Fact]
        public async Task UpdateAsync_OnUpdate_ShouldUseSharedTimestamp()
        {
            var categories = new List<Category>
            {
                new Category { Name = "Test Category 1" },
                new Category { Name = "Test Category 2" }
            };

            var categoryKeys = new List<string>();
            foreach (var category in categories)
            {
                var createdCategory = await _fixture.CategoriesClient.CreateCategoryAsync(category);
                categoryKeys.Add(createdCategory.Key);
            }

            var createViewRequest = new View
            {
                Name = "Initial View",
                Categories = { categoryKeys[0] }
            };
            var createdView = await _fixture.Client.CreateViewAsync(createViewRequest);

            var updateRequest = new ViewActionRequest
            {
                Key = createdView.Key,
                View = new View { Name = "Updated View", Categories = { categoryKeys[1] } }
            };
            var response = await _fixture.Client.UpdateViewAsync(updateRequest);


            var createTimestamp = createdView.View.Categories.Select(x => x.Revision.CreatedAt).ToList();
            createTimestamp.Add(createdView.Revision.CreatedAt);

            var updateTimestamp = response.View.Categories.Select(x => x.Revision.CreatedAt).ToList();
            updateTimestamp.Add(response.Revision.CreatedAt);

            Assert.Single(createTimestamp.Distinct());
            Assert.Single(updateTimestamp.Distinct());
        }
    }
}
