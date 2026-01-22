using FluentValidation;
using Mkat.Application.DTOs;
using Mkat.Domain.Enums;

namespace Mkat.Application.Validators;

public class CreateServiceValidator : AbstractValidator<CreateServiceRequest>
{
    public CreateServiceValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must be 100 characters or less");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must be 500 characters or less");

        RuleFor(x => x.Severity)
            .IsInEnum().WithMessage("Invalid severity value");

        RuleFor(x => x.Monitors)
            .NotEmpty().WithMessage("At least one monitor is required");

        RuleForEach(x => x.Monitors).SetValidator(new CreateMonitorValidator());
    }
}

public class CreateMonitorValidator : AbstractValidator<CreateMonitorRequest>
{
    public CreateMonitorValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid monitor type")
            .Must(t => t != MonitorType.HealthCheck)
            .WithMessage("Health check monitors are not supported yet");

        RuleFor(x => x.IntervalSeconds)
            .GreaterThanOrEqualTo(30).WithMessage("Interval must be at least 30 seconds")
            .LessThanOrEqualTo(604800).WithMessage("Interval must be 7 days or less");

        RuleFor(x => x.GracePeriodSeconds)
            .GreaterThanOrEqualTo(60).When(x => x.GracePeriodSeconds.HasValue)
            .WithMessage("Grace period must be at least 60 seconds");
    }
}
