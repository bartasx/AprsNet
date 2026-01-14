using Aprs.Application.Packets.DTOs;
using Aprs.Application.Packets.Mappings;
using Aprs.Domain.Enums;
using Aprs.Domain.Interfaces;
using MediatR;

namespace Aprs.Application.Packets.Queries.GetPackets;

public record GetPacketsQuery(
    string? Sender = null,
    PacketType? Type = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 100
) : IRequest<GetPacketsResponse>;

public record GetPacketsResponse(
    IReadOnlyList<PacketDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage
);

public class GetPacketsHandler : IRequestHandler<GetPacketsQuery, GetPacketsResponse>
{
    private readonly IPacketRepository _repository;

    public GetPacketsHandler(IPacketRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetPacketsResponse> Handle(GetPacketsQuery request, CancellationToken cancellationToken)
    {
        var (packets, totalCount) = await _repository.SearchWithCountAsync(
            request.Sender,
            request.Type,
            request.From,
            request.To,
            request.Page,
            request.PageSize,
            cancellationToken
        );

        var dtos = packets.ToDto().ToList();
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new GetPacketsResponse(
            Items: dtos,
            Page: request.Page,
            PageSize: request.PageSize,
            TotalCount: totalCount,
            TotalPages: totalPages,
            HasNextPage: request.Page < totalPages,
            HasPreviousPage: request.Page > 1
        );
    }
}
