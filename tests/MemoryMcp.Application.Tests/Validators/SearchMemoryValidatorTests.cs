using Xunit;
using FluentAssertions;
using FluentValidation.TestHelper;
using MemoryMcp.Application.DTOs;
using MemoryMcp.Application.Validators;

namespace MemoryMcp.Application.Tests.Validators;

public class SearchMemoryValidatorTests
{
    private readonly SearchMemoryValidator _validator = new();

    [Fact]
    public void Validate_SemanticSearchWithQuery_HasNoErrors()
    {
        var request = new SearchMemoryRequest
        {
            Query = "test query",
            SemanticSearch = true,
            TopK = 10
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_SemanticSearchWithoutQuery_HasValidationError()
    {
        var request = new SearchMemoryRequest
        {
            Query = "",
            SemanticSearch = true
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Query);
    }

    [Fact]
    public void Validate_TopKOutOfRange_HasValidationError()
    {
        var request = new SearchMemoryRequest
        {
            Query = "test",
            TopK = 0
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.TopK);
    }
}
