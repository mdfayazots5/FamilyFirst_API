namespace FamilyFirst.Application.DTOs.Medical;

public sealed record HeightWeightDto(
    Guid HeightWeightRecordId,
    DateOnly RecordedDate,
    decimal? HeightCm,
    decimal? WeightKg
);

public sealed record AddHeightWeightRequest(
    DateOnly RecordedDate,
    decimal? HeightCm,
    decimal? WeightKg
);
