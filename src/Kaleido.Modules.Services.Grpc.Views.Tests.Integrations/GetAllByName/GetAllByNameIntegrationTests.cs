using Grpc.Core;
using Kaleido.Grpc.Categories;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Tests.Integrations.Fixtures;
using Xunit;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Integrations.GetAllByName;

public class GetAllByNameIntegrationTests : IClassFixture<InfrastructureFixture>
{
    private readonly InfrastructureFixture _fixture;

    public GetAllByNameIntegrationTests(InfrastructureFixture fixture)
    {
        _fixture = fixture;
        _fixture.ClearDatabase().Wait();
    }

    [Fact]
    public async Task GetAllByName_WhenViewsExist_ReturnsViewListResponse()
    {
        // Arrange
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
        await _fixture.Client.CreateViewAsync(createView);

        var request = new GetAllViewsByNameRequest { Name = "Test View" };

        // Act
        var response = await _fixture.Client.GetAllViewsByNameAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Views);
        Assert.Equal("Test View", response.Views.First().View.Name);
    }

    [Fact]
    public async Task GetAllByName_WhenNoViewsExist_ReturnsEmptyResponse()
    {
        // Arrange
        var request = new GetAllViewsByNameRequest { Name = "Nonexistent View" };

        // Act
        var response = await _fixture.Client.GetAllViewsByNameAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Empty(response.Views);
    }

    [Fact]
    public async Task GetAllByName_WhenViewIsDeleted_DoesNotIncludeIt()
    {
        // Arrange
        var createView = new View
        {
            Name = "Test View to Delete",
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

        var deleteViewRequest = new ViewRequest { Key = createdView.Key };
        await _fixture.Client.DeleteViewAsync(deleteViewRequest);

        var request = new GetAllViewsByNameRequest { Name = "Test View to Delete" };

        // Act
        var response = await _fixture.Client.GetAllViewsByNameAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Empty(response.Views);
    }

    [Fact]
    public async Task GetAllByName_ReturnsPartialMatches()
    {
        // Arrange
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
        await _fixture.Client.CreateViewAsync(createView);

        var request = new GetAllViewsByNameRequest { Name = "Test" };

        // Act
        var response = await _fixture.Client.GetAllViewsByNameAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Views);
        Assert.Equal(createView.Name, response.Views.First().View.Name);
    }

    [Fact]
    public async Task GetAllByName_IsCaseInsensitive()
    {
        // Arrange
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
        await _fixture.Client.CreateViewAsync(createView);

        var request = new GetAllViewsByNameRequest { Name = "test" };

        // Act
        var response = await _fixture.Client.GetAllViewsByNameAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Views);
        Assert.Equal(createView.Name, response.Views.First().View.Name);
    }
}

