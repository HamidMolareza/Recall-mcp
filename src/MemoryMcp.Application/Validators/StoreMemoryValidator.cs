using FluentValidation;
using MemoryMcp.Application.DTOs;

namespace MemoryMcp.Application.Validators;

public sealed class StoreMemoryValidator : AbstractValidator<StoreMemoryRequest>
{
    public StoreMemoryValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(50_000);
        RuleFor(x => x.Namespace).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.ExpiresAt.HasValue);
    }
}
