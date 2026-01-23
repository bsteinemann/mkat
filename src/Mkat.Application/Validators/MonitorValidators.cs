using FluentValidation;
using Mkat.Application.DTOs;
using Mkat.Domain.Enums;

namespace Mkat.Application.Validators;

public class AddMonitorValidator : AbstractValidator<AddMonitorRequest>
{
    public AddMonitorValidator()
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

public class UpdateMonitorValidator : AbstractValidator<UpdateMonitorRequest>
{
    public UpdateMonitorValidator()
    {
        RuleFor(x => x.IntervalSeconds)
            .GreaterThanOrEqualTo(30).WithMessage("Interval must be at least 30 seconds")
            .LessThanOrEqualTo(604800).WithMessage("Interval must be 7 days or less");

        RuleFor(x => x.GracePeriodSeconds)
            .GreaterThanOrEqualTo(60).When(x => x.GracePeriodSeconds.HasValue)
            .WithMessage("Grace period must be at least 60 seconds");
    }
}
