using FluentValidation;
using Mkat.Application.DTOs;
using Mkat.Domain.Enums;

namespace Mkat.Application.Validators;

public class AddMonitorValidator : AbstractValidator<AddMonitorRequest>
{
    private static readonly string[] ValidHttpMethods = ["GET", "HEAD", "POST", "PUT"];

    public AddMonitorValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid monitor type");

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

        // Health check monitor validation rules
        When(x => x.Type == MonitorType.HealthCheck, () =>
        {
            RuleFor(x => x.HealthCheckUrl)
                .NotEmpty().WithMessage("HealthCheckUrl is required for health check monitors");

            RuleFor(x => x.HealthCheckUrl)
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                    && (uri.Scheme == "http" || uri.Scheme == "https"))
                .When(x => !string.IsNullOrEmpty(x.HealthCheckUrl))
                .WithMessage("HealthCheckUrl must be a valid HTTP or HTTPS URL");

            RuleFor(x => x.HttpMethod)
                .Must(m => ValidHttpMethods.Contains(m, StringComparer.OrdinalIgnoreCase))
                .When(x => !string.IsNullOrEmpty(x.HttpMethod))
                .WithMessage("HttpMethod must be one of: GET, HEAD, POST, PUT");

            RuleFor(x => x.ExpectedStatusCodes)
                .Must(codes =>
                {
                    if (string.IsNullOrEmpty(codes)) return true;
                    return codes.Split(',').All(c =>
                        int.TryParse(c.Trim(), out var code) && code >= 100 && code <= 599);
                })
                .WithMessage("ExpectedStatusCodes must be comma-separated integers between 100 and 599");

            RuleFor(x => x.TimeoutSeconds)
                .InclusiveBetween(1, 120)
                .When(x => x.TimeoutSeconds.HasValue)
                .WithMessage("TimeoutSeconds must be between 1 and 120");

            RuleFor(x => x.BodyMatchRegex)
                .Must(pattern =>
                {
                    try { _ = new System.Text.RegularExpressions.Regex(pattern!); return true; }
                    catch { return false; }
                })
                .When(x => !string.IsNullOrEmpty(x.BodyMatchRegex))
                .WithMessage("BodyMatchRegex must be a valid regular expression");
        });
    }
}

public class UpdateMonitorValidator : AbstractValidator<UpdateMonitorRequest>
{
    private static readonly string[] ValidHttpMethods = ["GET", "HEAD", "POST", "PUT"];

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

        // Health check monitor validation rules
        When(x => x.Type == MonitorType.HealthCheck, () =>
        {
            RuleFor(x => x.HealthCheckUrl)
                .NotEmpty().WithMessage("HealthCheckUrl is required for health check monitors");

            RuleFor(x => x.HealthCheckUrl)
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                    && (uri.Scheme == "http" || uri.Scheme == "https"))
                .When(x => !string.IsNullOrEmpty(x.HealthCheckUrl))
                .WithMessage("HealthCheckUrl must be a valid HTTP or HTTPS URL");

            RuleFor(x => x.HttpMethod)
                .Must(m => ValidHttpMethods.Contains(m, StringComparer.OrdinalIgnoreCase))
                .When(x => !string.IsNullOrEmpty(x.HttpMethod))
                .WithMessage("HttpMethod must be one of: GET, HEAD, POST, PUT");

            RuleFor(x => x.ExpectedStatusCodes)
                .Must(codes =>
                {
                    if (string.IsNullOrEmpty(codes)) return true;
                    return codes.Split(',').All(c =>
                        int.TryParse(c.Trim(), out var code) && code >= 100 && code <= 599);
                })
                .WithMessage("ExpectedStatusCodes must be comma-separated integers between 100 and 599");

            RuleFor(x => x.TimeoutSeconds)
                .InclusiveBetween(1, 120)
                .When(x => x.TimeoutSeconds.HasValue)
                .WithMessage("TimeoutSeconds must be between 1 and 120");

            RuleFor(x => x.BodyMatchRegex)
                .Must(pattern =>
                {
                    try { _ = new System.Text.RegularExpressions.Regex(pattern!); return true; }
                    catch { return false; }
                })
                .When(x => !string.IsNullOrEmpty(x.BodyMatchRegex))
                .WithMessage("BodyMatchRegex must be a valid regular expression");
        });
    }
}
