using FluentValidation;
using Grpc.Core;
using Kaleido.Grpc.Categories;
using Kaleido.Grpc.Views;
using Kaleido.Modules.Services.Grpc.Views.Common.Models;
using static Kaleido.Grpc.Categories.GrpcCategories;

namespace Kaleido.Modules.Services.Grpc.Views.Common.Validators;

public class ViewValidator : AbstractValidator<View>
{
    private readonly GrpcCategoriesClient _client;
    private readonly ILogger<ViewValidator> _logger;

    public ViewValidator(GrpcCategoriesClient client, ILogger<ViewValidator> logger)
    {
        _client = client;
        _logger = logger;

        RuleFor(x => x.Name).SetValidator(new NameValidator());
        RuleFor(x => x.Categories).NotNull().NotEmpty().MustAsync(ValidateCategories).WithMessage("One or more categories do not exist");
    }

    private async Task<bool> ValidateCategories(IEnumerable<string> categories, CancellationToken cancellationToken)
    {
        foreach (var category in categories)
        {
            try
            {
                _logger.LogInformation("Validating category {Category}", category);
                var result = await _client.GetCategoryAsync(new CategoryRequest { Key = category }, cancellationToken: cancellationToken);
                _logger.LogInformation("Category {Category} exists", category);
            }
            catch (RpcException e)
            {
                _logger.LogError(e, "Category {Category} does not exist", category);
                return false;
            }
        }
        return true;
    }
}