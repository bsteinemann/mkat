using FluentValidation;
using Mkat.Application.DTOs;

namespace Mkat.Application.Validators;

public class PeerInitiateValidator : AbstractValidator<PeerInitiateRequest>
{
    public PeerInitiateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");
    }
}

public class PeerAcceptValidator : AbstractValidator<PeerAcceptRequest>
{
    public PeerAcceptValidator()
    {
        RuleFor(x => x.Secret)
            .NotEmpty().WithMessage("Secret is required");

        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("URL is required")
            .MaximumLength(500).WithMessage("URL must not exceed 500 characters");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");
    }
}

public class PeerCompleteValidator : AbstractValidator<PeerCompleteRequest>
{
    public PeerCompleteValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required");
    }
}
