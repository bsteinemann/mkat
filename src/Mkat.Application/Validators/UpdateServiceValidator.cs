using FluentValidation;
using Mkat.Application.DTOs;

namespace Mkat.Application.Validators;

public class UpdateServiceValidator : AbstractValidator<UpdateServiceRequest>
{
    public UpdateServiceValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must be 100 characters or less");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must be 500 characters or less");

        RuleFor(x => x.Severity)
            .IsInEnum().WithMessage("Invalid severity value");
    }
}
