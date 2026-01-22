using Mkat.Application.DTOs;
using Mkat.Application.Validators;
using Mkat.Domain.Enums;
using Xunit;

namespace Mkat.Application.Tests.Validators;

public class UpdateServiceValidatorTests
{
    private readonly UpdateServiceValidator _validator = new();

    [Fact]
    public async Task ValidRequest_Passes()
    {
        var request = new UpdateServiceRequest
        {
            Name = "Updated Service",
            Description = "Updated description",
            Severity = Severity.High
        };

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task EmptyName_Fails()
    {
        var request = new UpdateServiceRequest
        {
            Name = "",
            Severity = Severity.Medium
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task NameTooLong_Fails()
    {
        var request = new UpdateServiceRequest
        {
            Name = new string('a', 101),
            Severity = Severity.Medium
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task DescriptionTooLong_Fails()
    {
        var request = new UpdateServiceRequest
        {
            Name = "Valid Name",
            Description = new string('a', 501),
            Severity = Severity.Medium
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Description");
    }
}
