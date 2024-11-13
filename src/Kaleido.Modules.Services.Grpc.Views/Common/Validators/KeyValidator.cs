using FluentValidation;

namespace Kaleido.Modules.Services.Grpc.Views.Common.Validators;

public class KeyValidator : AbstractValidator<string>
{
    public KeyValidator()
    {
        RuleFor(x => x).NotNull().NotEmpty().Must(x => Guid.TryParse(x, out _));
    }
}