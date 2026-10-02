namespace SwiftBets.Cashout.Domain;

/// <summary>
/// The cashout value of a single or accumulator: what has already won stays won, each open leg is bought back at the
/// ratio of its placed to its live odds, and the house keeps a margin. A void leg counts at 1.00. The value never
/// exceeds what the bet could still pay, and rounds down to the minor unit.
/// </summary>
public static class CashoutPricer
{
    public static long? Price(long stake, IReadOnlyList<PricedLeg> legs, decimal margin)
    {
        ArgumentNullException.ThrowIfNull(legs);
        if (legs.Count == 0 || legs.Any(l => l.Status == LegStatus.Lost) || margin is < 0 or >= 1)
        {
            return null;
        }

        var open = legs.Where(l => l.Status == LegStatus.Open).ToList();
        if (open.Count == 0 || open.Any(l => l.CurrentOdds is not > 1m))
        {
            return null;
        }

        var factor = 1m;
        var potential = 1m;
        foreach (var leg in legs)
        {
            var placed = leg.Status == LegStatus.Void ? 1m : leg.PlacedOdds;
            potential *= placed;
            factor *= leg.Status == LegStatus.Open ? placed / leg.CurrentOdds!.Value : placed;
        }

        var value = Math.Min(stake * factor * (1 - margin), stake * potential);
        return (long)decimal.Floor(value);
    }
}
