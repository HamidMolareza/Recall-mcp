using FluentValidation;
using MemoryMcp.Application.DTOs;

namespace MemoryMcp.Application.Validators;

public sealed class SearchMemoryValidator : AbstractValidator<SearchMemoryRequest>
{
    public SearchMemoryValidator()
    {
        RuleFor(x => x.TopK).InclusiveBetween(1, 100);
        RuleFor(x => x.Query)
            .NotEmpty()
            .When(x => x.SemanticSearch);
    }
}
