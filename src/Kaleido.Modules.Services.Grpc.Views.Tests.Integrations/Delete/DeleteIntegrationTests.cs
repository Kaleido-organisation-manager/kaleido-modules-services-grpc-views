using Grpc.Core;
using Kaleido.Grpc.Categories;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Tests.Integrations.Fixtures;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Integration.Delete;

[Collection("Infrastructure collection")]
public class DeleteIntegrationTests
{
    private readonly InfrastructureFixture _fixture;

    public DeleteIntegrationTests(InfrastructureFixture fixture)
    {
        _fixture = fixture;
        _fixture.ClearDatabase().Wait();
    }

    [Fact]
    public async Task Delete_WhenViewDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var request = new ViewRequest { Key = Guid.NewGuid().ToString() };

        // Act
        var act = async () => await _fixture.Client.DeleteViewAsync(request);

        // Assert
        var exception = await Assert.ThrowsAsync<RpcException>(() => act());
        Assert.Equal(StatusCode.NotFound, exception.Status.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenViewExists_DeletesViewAndLinks()
    {
        // Arrange
        // First create a view with categories
        var createView = new View
        {
            Name = "Test View",
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
        var deleteRequest = new ViewRequest { Key = createdView.Key };
        var deletedView = await _fixture.Client.DeleteViewAsync(deleteRequest);

        // Assert
        Assert.NotNull(deletedView);
        Assert.Equal(createdView.Key, deletedView.Key);

        // Verify the view is actually deleted by trying to get it
        var getRequest = new ViewRequest { Key = createdView.Key };
        var act = async () => await _fixture.Client.GetViewAsync(getRequest);
        var exception = await Assert.ThrowsAsync<RpcException>(() => act());
        Assert.Equal(StatusCode.NotFound, exception.Status.StatusCode);
    }

    [Fact]
    public async Task Delete_WithInvalidKey_ThrowsInvalidArgumentException()
    {
        // Arrange
        var request = new ViewRequest { Key = "invalid-guid" };

        // Act
        var act = async () => await _fixture.Client.DeleteViewAsync(request);

        // Assert
        var exception = await Assert.ThrowsAsync<RpcException>(() => act());
        Assert.Equal(StatusCode.InvalidArgument, exception.Status.StatusCode);
    }
}