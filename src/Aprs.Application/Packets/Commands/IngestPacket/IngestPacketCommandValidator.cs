using Aprs.Application.Packets.Commands.IngestPacket;
using FluentValidation;

namespace Aprs.Application.Packets.Commands.IngestPacket;

/// <summary>
/// Validator for <see cref="IngestPacketCommand"/>.
/// </summary>
public sealed class IngestPacketCommandValidator : AbstractValidator<IngestPacketCommand>
{
    public IngestPacketCommandValidator()
    {
        RuleFor(x => x.Packet)
            .NotNull()
            .WithMessage("Packet cannot be null");

        RuleFor(x => x.Packet.Sender)
            .NotNull()
            .When(x => x.Packet != null)
            .WithMessage("Packet sender is required");

        RuleFor(x => x.Packet.RawContent)
            .NotEmpty()
            .When(x => x.Packet != null)
            .WithMessage("Packet raw content is required");

        RuleFor(x => x.Packet.RawContent)
            .MaximumLength(1024)
            .When(x => x.Packet?.RawContent != null)
            .WithMessage("Packet raw content cannot exceed 1024 characters");
    }
}
