using Kaleido.Grpc.Categories;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Tests.Integrations.Fixtures;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Integrations.GetAllRevisions;

[Collection("Infrastructure collection")]
public class GetAllRevisionsIntegrationTests
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

        // Create revisions for the view
        var updatedView = new View { Name = "Updated View", Categories = { categoryKeys.First() } };
        await _fixture.Client.UpdateViewAsync(new ViewActionRequest { Key = createdView.Key, View = updatedView });

        // Act
        var request = new ViewRequest { Key = createdView.Key };
        var response = await _fixture.Client.GetAllViewRevisionsAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Views);
        Assert.Equal("Updated View", response.Views.First().View.Name);
        Assert.Equal("Test View", response.Views.Skip(1).First().View.Name);
        Assert.Equal(createdView.Key, response.Views.First().Key);
        Assert.Equal(2, response.Views.Count); // Expecting 2 revisions
    }

    [Fact]
    public async Task GetAllRevisions_WhenNoRevisionsExist_ReturnsEmptyResponse()
    {
        // Arrange
        var key = Guid.NewGuid();

        // Act
        var request = new ViewRequest { Key = key.ToString() };
        var response = await _fixture.Client.GetAllViewRevisionsAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Empty(response.Views);
    }

    [Fact]
    public async Task GetAllRevisions_WhenViewIsDeleted_DoesIncludeIt()
    {
        // Arrange
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

        var createView = new View
        {
            Name = "Test View to Delete",
            Categories = { categoryKeys.First() }
        };

        var createdView = await _fixture.Client.CreateViewAsync(createView);
        await _fixture.Client.DeleteViewAsync(new ViewRequest { Key = createdView.Key });

        // Act
        var request = new ViewRequest { Key = createdView.Key };
        var response = await _fixture.Client.GetAllViewRevisionsAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Views);
        Assert.Equal(2, response.Views.Count);
    }

    [Fact]
    public async Task GetAllRevisions_ReflectsCategoryChanges()
    {
        var categories = new List<Category>
        {
            new Category { Name = "Category 1" },
            new Category { Name = "Category 2" },
            new Category { Name = "Category 3" }
        };

        var categoryKeys = new List<string>();
        foreach (var category in categories)
        {
            var createdCategory = await _fixture.CategoriesClient.CreateCategoryAsync(category);
            categoryKeys.Add(createdCategory.Key);
        }

        var createView = new View
        {
            Name = "Test View",
            Categories = { categoryKeys[0], categoryKeys[1] }
        };

        var createdView = await _fixture.Client.CreateViewAsync(createView);

        var updateView = new View
        {
            Name = createView.Name,
            Categories = { categoryKeys[0] }
        };

        await _fixture.Client.UpdateViewAsync(new ViewActionRequest { Key = createdView.Key, View = updateView });

        var updateView2 = new View
        {
            Name = createView.Name,
            Categories = { categoryKeys[0], categoryKeys[2] }
        };

        await _fixture.Client.UpdateViewAsync(new ViewActionRequest { Key = createdView.Key, View = updateView2 });

        var request = new ViewRequest { Key = createdView.Key };
        var response = await _fixture.Client.GetAllViewRevisionsAsync(request);

        Assert.Equal(3, response.Views.Count);
        Assert.Equal("Unmodified", response.Views.First().Revision.Action);
        Assert.Equal("Unmodified", response.Views.Skip(1).First().Revision.Action);
        Assert.Equal("Created", response.Views.Last().Revision.Action);

        Assert.Equal(2, response.Views.Last().View.Categories.Count);
        Assert.Contains(categoryKeys[1], response.Views.Last().View.Categories.Select(c => c.CategoryLink.Category));
        Assert.Contains(categoryKeys[0], response.Views.Last().View.Categories.Select(c => c.CategoryLink.Category));
        Assert.Equal("Created", response.Views.Last().View.Categories.FirstOrDefault(x => x.CategoryLink.Category == categoryKeys[0])?.Revision.Action);
        Assert.Equal("Created", response.Views.Last().View.Categories.FirstOrDefault(x => x.CategoryLink.Category == categoryKeys[1])?.Revision.Action);

        Assert.Equal(2, response.Views[1].View.Categories.Count);
        Assert.Contains(categoryKeys[0], response.Views.Skip(1).First().View.Categories.Select(c => c.CategoryLink.Category));
        Assert.Contains(categoryKeys[1], response.Views.Skip(1).First().View.Categories.Select(c => c.CategoryLink.Category));
        Assert.Equal("Unmodified", response.Views.Skip(1).First().View.Categories.FirstOrDefault(x => x.CategoryLink.Category == categoryKeys[0])?.Revision.Action);
        Assert.Equal("Deleted", response.Views.Skip(1).First().View.Categories.FirstOrDefault(x => x.CategoryLink.Category == categoryKeys[1])?.Revision.Action);

        Assert.Equal(2, response.Views.First().View.Categories.Count);
        Assert.Contains(categoryKeys[0], response.Views.First().View.Categories.Select(c => c.CategoryLink.Category));
        Assert.Contains(categoryKeys[2], response.Views.First().View.Categories.Select(c => c.CategoryLink.Category));
        Assert.Equal("Unmodified", response.Views.First().View.Categories.FirstOrDefault(x => x.CategoryLink.Category == categoryKeys[0])?.Revision.Action);
        Assert.Equal("Created", response.Views.First().View.Categories.FirstOrDefault(x => x.CategoryLink.Category == categoryKeys[2])?.Revision.Action);
    }

    [Fact]
    public async Task GetAllRevisions_ReflectsCategoryAndViewChanges()
    {
        var categories = new List<Category>
        {
            new Category { Name = "Category 1" },
            new Category { Name = "Category 2" },
            new Category { Name = "Category 3" }
        };

        var categoryKeys = new List<string>();
        foreach (var category in categories)
        {
            var createdCategory = await _fixture.CategoriesClient.CreateCategoryAsync(category);
            categoryKeys.Add(createdCategory.Key);
        }

        var createView = new View
        {
            Name = "Test View",
            Categories = { categoryKeys[0], categoryKeys[1] }
        };

        var createdView = await _fixture.Client.CreateViewAsync(createView);

        var updateView = new View
        {
            Name = "Updated View",
            Categories = { categoryKeys[0] }
        };

        await _fixture.Client.UpdateViewAsync(new ViewActionRequest { Key = createdView.Key, View = updateView });

        var updateView2 = new View
        {
            Name = "Updated View 2",
            Categories = { categoryKeys[0], categoryKeys[2] }
        };

        await _fixture.Client.UpdateViewAsync(new ViewActionRequest { Key = createdView.Key, View = updateView2 });

        var request = new ViewRequest { Key = createdView.Key };
        var response = await _fixture.Client.GetAllViewRevisionsAsync(request);

        Assert.Equal(3, response.Views.Count);
        Assert.Equal("Updated", response.Views.First().Revision.Action);
        Assert.Equal("Updated", response.Views.Skip(1).First().Revision.Action);
        Assert.Equal("Created", response.Views.Last().Revision.Action);

        var createRevision = response.Views.Last();
        var updateRevision = response.Views.Skip(1).First();
        var update2Revision = response.Views.First();

        Assert.Equal(2, createRevision.View.Categories.Count);
        Assert.Contains(categoryKeys[1], createRevision.View.Categories.Select(c => c.CategoryLink.Category));
        Assert.Contains(categoryKeys[0], createRevision.View.Categories.Select(c => c.CategoryLink.Category));
        Assert.Equal("Created", createRevision.View.Categories.FirstOrDefault(x => x.CategoryLink.Category == categoryKeys[0])?.Revision.Action);
        Assert.Equal("Created", createRevision.View.Categories.FirstOrDefault(x => x.CategoryLink.Category == categoryKeys[1])?.Revision.Action);

        Assert.Equal(2, updateRevision.View.Categories.Count);
        Assert.Contains(categoryKeys[0], updateRevision.View.Categories.Select(c => c.CategoryLink.Category));
        Assert.Contains(categoryKeys[1], updateRevision.View.Categories.Select(c => c.CategoryLink.Category));
        Assert.Equal("Unmodified", updateRevision.View.Categories.FirstOrDefault(x => x.CategoryLink.Category == categoryKeys[0])?.Revision.Action);
        Assert.Equal("Deleted", updateRevision.View.Categories.FirstOrDefault(x => x.CategoryLink.Category == categoryKeys[1])?.Revision.Action);

        Assert.Equal(2, update2Revision.View.Categories.Count);
        Assert.Contains(categoryKeys[0], update2Revision.View.Categories.Select(c => c.CategoryLink.Category));
        Assert.Contains(categoryKeys[2], update2Revision.View.Categories.Select(c => c.CategoryLink.Category));
        Assert.Equal("Unmodified", update2Revision.View.Categories.FirstOrDefault(x => x.CategoryLink.Category == categoryKeys[0])?.Revision.Action);
        Assert.Equal("Created", update2Revision.View.Categories.FirstOrDefault(x => x.CategoryLink.Category == categoryKeys[2])?.Revision.Action);
    }
}
