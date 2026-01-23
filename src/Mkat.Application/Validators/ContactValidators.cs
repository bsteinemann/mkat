using FluentValidation;
using Mkat.Application.DTOs;

namespace Mkat.Application.Validators;

public class CreateContactValidator : AbstractValidator<CreateContactRequest>
{
    public CreateContactValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public class UpdateContactValidator : AbstractValidator<UpdateContactRequest>
{
    public UpdateContactValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public class AddChannelValidator : AbstractValidator<AddChannelRequest>
{
    public AddChannelValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Configuration).NotEmpty().MaximumLength(4000);
    }
}

public class UpdateChannelValidator : AbstractValidator<UpdateChannelRequest>
{
    public UpdateChannelValidator()
    {
        RuleFor(x => x.Configuration).NotEmpty().MaximumLength(4000);
    }
}

public class SetServiceContactsValidator : AbstractValidator<SetServiceContactsRequest>
{
    public SetServiceContactsValidator()
    {
        RuleFor(x => x.ContactIds).NotEmpty()
            .WithMessage("At least one contact is required.");
    }
}
