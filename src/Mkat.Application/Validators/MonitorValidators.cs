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

        // Metric monitor validation rules
        When(x => x.Type == MonitorType.Metric, () =>
        {
            RuleFor(x => x)
                .Must(x => x.MinValue.HasValue || x.MaxValue.HasValue)
                .WithMessage("At least one of MinValue or MaxValue is required for metric monitors");

            RuleFor(x => x.MaxValue)
                .GreaterThan(x => x.MinValue!.Value)
                .When(x => x.MinValue.HasValue && x.MaxValue.HasValue)
                .WithMessage("MaxValue must be greater than MinValue");

            RuleFor(x => x.ThresholdCount)
                .NotNull().WithMessage("ThresholdCount is required for ConsecutiveCount strategy")
                .GreaterThan(0).WithMessage("ThresholdCount must be greater than 0")
                .When(x => x.ThresholdStrategy == ThresholdStrategy.ConsecutiveCount);

            RuleFor(x => x.WindowSeconds)
                .NotNull().WithMessage("WindowSeconds is required for TimeDurationAverage strategy")
                .GreaterThan(0).WithMessage("WindowSeconds must be greater than 0")
                .When(x => x.ThresholdStrategy == ThresholdStrategy.TimeDurationAverage);

            RuleFor(x => x.WindowSampleCount)
                .NotNull().WithMessage("WindowSampleCount is required for SampleCountAverage strategy")
                .GreaterThan(0).WithMessage("WindowSampleCount must be greater than 0")
                .When(x => x.ThresholdStrategy == ThresholdStrategy.SampleCountAverage);

            RuleFor(x => x.RetentionDays)
                .InclusiveBetween(1, 365)
                .WithMessage("RetentionDays must be between 1 and 365");
        });
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

        // Metric monitor validation rules
        When(x => x.Type == MonitorType.Metric, () =>
        {
            RuleFor(x => x)
                .Must(x => x.MinValue.HasValue || x.MaxValue.HasValue)
                .WithMessage("At least one of MinValue or MaxValue is required for metric monitors");

            RuleFor(x => x.MaxValue)
                .GreaterThan(x => x.MinValue!.Value)
                .When(x => x.MinValue.HasValue && x.MaxValue.HasValue)
                .WithMessage("MaxValue must be greater than MinValue");

            RuleFor(x => x.ThresholdCount)
                .NotNull().WithMessage("ThresholdCount is required for ConsecutiveCount strategy")
                .GreaterThan(0).WithMessage("ThresholdCount must be greater than 0")
                .When(x => x.ThresholdStrategy == ThresholdStrategy.ConsecutiveCount);

            RuleFor(x => x.WindowSeconds)
                .NotNull().WithMessage("WindowSeconds is required for TimeDurationAverage strategy")
                .GreaterThan(0).WithMessage("WindowSeconds must be greater than 0")
                .When(x => x.ThresholdStrategy == ThresholdStrategy.TimeDurationAverage);

            RuleFor(x => x.WindowSampleCount)
                .NotNull().WithMessage("WindowSampleCount is required for SampleCountAverage strategy")
                .GreaterThan(0).WithMessage("WindowSampleCount must be greater than 0")
                .When(x => x.ThresholdStrategy == ThresholdStrategy.SampleCountAverage);

            RuleFor(x => x.RetentionDays)
                .InclusiveBetween(1, 365)
                .When(x => x.RetentionDays.HasValue)
                .WithMessage("RetentionDays must be between 1 and 365");
        });
    }
}
