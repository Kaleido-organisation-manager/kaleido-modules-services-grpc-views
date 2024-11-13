using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Kaleido.Grpc.Categories;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Tests.Integrations.Fixtures;
using Xunit;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Integrations.GetRevision;

[Collection("Infrastructure collection")]
public class GetRevisionIntegrationTests
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

        var createView = new View
        {
            Name = "Test View",
            Categories = { categoryKeys }
        };

        var createdView = await _fixture.Client.CreateViewAsync(createView);

        // I don't know why I have to do this, but it works, a normal object assignment doesn't work
        var key = createdView.Key;
        var revisionRequest = new ViewActionRequest();
        revisionRequest.Key = key;
        var view = new View();
        view.Name = "Updated View";
        view.Categories.Add(categoryKeys.First());
        revisionRequest.View = view;

        await _fixture.Client.UpdateViewAsync(revisionRequest);

        // Act
        var request = new GetViewRevisionRequest { Key = key, CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow) };
        var response = await _fixture.Client.GetViewRevisionAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("Updated View", response.View.Name);
        Assert.Equal(key, response.Key);
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
