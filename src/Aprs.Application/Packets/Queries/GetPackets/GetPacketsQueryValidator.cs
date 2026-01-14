using Aprs.Application.Packets.Queries.GetPackets;
using FluentValidation;

namespace Aprs.Application.Packets.Queries.GetPackets;

/// <summary>
/// Validator for <see cref="GetPacketsQuery"/>.
/// </summary>
public sealed class GetPacketsQueryValidator : AbstractValidator<GetPacketsQuery>
{
    private const int MaxPageSize = 1000;
    private const int MinPageSize = 1;
    private const int MinPage = 1;

    public GetPacketsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(MinPage)
            .WithMessage($"Page must be at least {MinPage}");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(MinPageSize, MaxPageSize)
            .WithMessage($"PageSize must be between {MinPageSize} and {MaxPageSize}");

        RuleFor(x => x.From)
            .LessThanOrEqualTo(x => x.To)
            .When(x => x.From.HasValue && x.To.HasValue)
            .WithMessage("'From' date must be earlier than or equal to 'To' date");

        RuleFor(x => x.Sender)
            .MaximumLength(15)
            .When(x => !string.IsNullOrEmpty(x.Sender))
            .WithMessage("Sender callsign cannot exceed 15 characters");

        RuleFor(x => x.Sender)
            .Matches(@"^[A-Z0-9]{1,6}(-[0-9]{1,2})?$")
            .When(x => !string.IsNullOrEmpty(x.Sender))
            .WithMessage("Invalid callsign format");
    }
}
