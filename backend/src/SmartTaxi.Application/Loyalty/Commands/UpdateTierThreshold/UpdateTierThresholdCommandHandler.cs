using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Commands.UpdateTierThreshold;

/// <summary>
/// Retuning a threshold never rewrites ledger history or past LoyaltyTierChanged
/// outcomes — it only changes what a given StatusPoints total maps to from this
/// point forward (see LoyaltyTierCalculator). Validates the FULL resulting
/// threshold set (existing rows + this proposed change) is strictly increasing
/// by tier rank before persisting — LoyaltyTierCalculator picks the
/// highest-reached threshold, so an inverted configuration (e.g. Gold below
/// Silver) would silently assign users the wrong tier (Module 7 audit finding
/// #5). Only tiers that actually have a configured row are compared, in
/// LoyaltyTier's own ordinal rank order (Bronze &lt; Silver &lt; Gold &lt; Platinum).
/// </summary>
public sealed class UpdateTierThresholdCommandHandler : ICommandHandler<UpdateTierThresholdCommand, Result>
{
    private const string InvalidOrderingError =
        "Les seuils de palier doivent être strictement croissants (Bronze < Silver < Gold < Platinum).";

    private readonly ILoyaltyTierThresholdRepository _repository;

    public UpdateTierThresholdCommandHandler(ILoyaltyTierThresholdRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(UpdateTierThresholdCommand command, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var existingThresholds = await _repository.GetAllAsync(cancellationToken);

        var simulated = existingThresholds
            .Where(existing => existing.Tier != command.Tier)
            .Select(existing => (existing.Tier, existing.MinimumStatusPoints))
            .Append((command.Tier, command.MinimumStatusPoints))
            .OrderBy(entry => (int)entry.Tier)
            .ToList();

        for (var i = 1; i < simulated.Count; i++)
        {
            if (simulated[i].MinimumStatusPoints <= simulated[i - 1].MinimumStatusPoints)
            {
                return Result.Failure(InvalidOrderingError, ErrorType.Validation);
            }
        }

        var threshold = existingThresholds.FirstOrDefault(existing => existing.Tier == command.Tier);

        try
        {
            if (threshold is null)
            {
                threshold = LoyaltyTierThreshold.Create(command.Tier, command.MinimumStatusPoints, utcNow);
                await _repository.AddAsync(threshold, cancellationToken);
            }
            else
            {
                threshold.UpdateThreshold(command.MinimumStatusPoints, utcNow);
                await _repository.UpdateAsync(threshold, cancellationToken);
            }
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message, ErrorType.Validation);
        }

        return Result.Success();
    }
}
