using Xunit;
using FluentAssertions;
using FluentValidation.TestHelper;
using MemoryMcp.Application.DTOs;
using MemoryMcp.Application.Validators;
using MemoryMcp.Domain.Enums;

namespace MemoryMcp.Application.Tests.Validators;

public class StoreMemoryValidatorTests
{
    private readonly StoreMemoryValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_HasNoErrors()
    {
        var request = new StoreMemoryRequest
        {
            Content = "Test content",
            Type = MemoryType.Fact,
            Namespace = "test-ns"
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyContent_HasValidationError()
    {
        var request = new StoreMemoryRequest
        {
            Content = "",
            Type = MemoryType.Fact,
            Namespace = "test-ns"
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Validate_EmptyNamespace_HasValidationError()
    {
        var request = new StoreMemoryRequest
        {
            Content = "Test",
            Type = MemoryType.Fact,
            Namespace = ""
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Namespace);
    }

    [Fact]
    public void Validate_PastExpiresAt_HasValidationError()
    {
        var request = new StoreMemoryRequest
        {
            Content = "Test",
            Type = MemoryType.Fact,
            Namespace = "test-ns",
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ExpiresAt);
    }
}
