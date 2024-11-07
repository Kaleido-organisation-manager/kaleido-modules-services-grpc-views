using Grpc.Core;
using Kaleido.Grpc.Categories;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Tests.Integrations.Fixtures;

namespace Kaleido.Modules.Services.Grpc.Views.Tests.Integrations.Create;

public class CreateIntegrationTests : IClassFixture<InfrastructureFixture>
{
    private readonly InfrastructureFixture _fixture;

    public CreateIntegrationTests(InfrastructureFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateView()
    {

        var categories = new List<Category> {
            new Category {
                Name = "Test"
            }
        };

        var resultCategories = new List<CategoryResponse>();
        foreach (var category in categories)
        {
            var result = await _fixture.CategoriesClient.CreateCategoryAsync(category);
            resultCategories.Add(result);
        }

        var response = await _fixture.Client.CreateViewAsync(new View
        {
            Name = "Test",
            Categories = { resultCategories.Select(c => c.Key).ToList() }
        });

        Assert.NotNull(response);
        Assert.Equal("Test", response.View.Name);
        Assert.NotNull(response.Revision);
        Assert.Equal("Created", response.Revision.Action);
        Assert.Equal(1, response.Revision.Revision);
    }

    [Fact]
    public async Task CreateAsync_CategoryDoesNotExist_ShouldThrow()
    {
        // Arrange
        var request = new View
        {
            Name = "Test",
            Categories = { Guid.NewGuid().ToString() }
        };

        // Act
        var exception = await Assert.ThrowsAsync<RpcException>(async () => await _fixture.Client.CreateViewAsync(request));

        // Assert
        Assert.Equal(StatusCode.InvalidArgument, exception.Status.StatusCode);
    }
}
