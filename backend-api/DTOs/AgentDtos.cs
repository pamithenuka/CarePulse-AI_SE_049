namespace CarePulse.Api.DTOs;

public record AgentSearchRequestDto(string Message);

public record AgentSearchResponseDto(
    string Reply,
    List<SlotSearchResultDto> MatchingSlots
);
