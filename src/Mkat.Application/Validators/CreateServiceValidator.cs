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
    private static readonly string[] ValidHttpMethods = ["GET", "HEAD", "POST", "PUT"];

    public CreateMonitorValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid monitor type");

        RuleFor(x => x.IntervalSeconds)
            .GreaterThanOrEqualTo(30).WithMessage("Interval must be at least 30 seconds")
            .LessThanOrEqualTo(604800).WithMessage("Interval must be 7 days or less");

        RuleFor(x => x.GracePeriodSeconds)
            .GreaterThanOrEqualTo(60).When(x => x.GracePeriodSeconds.HasValue)
            .WithMessage("Grace period must be at least 60 seconds");

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
