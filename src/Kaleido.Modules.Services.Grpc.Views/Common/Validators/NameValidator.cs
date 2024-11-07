using FluentValidation;

namespace Kaleido.Modules.Services.Grpc.Views.Common.Validators;

public class NameValidator : AbstractValidator<string>
{
    public NameValidator()
    {
        RuleFor(x => x).NotNull().NotEmpty().MaximumLength(100);
    }
}