namespace FamilyFirst.Domain.Enums;

// Privacy tier model — non-negotiable ethical foundation of the Finance module.
// Tiers cannot be configured below the minimums documented here.
public static class PrivacyTier
{
    // Full visibility — dependents, non-earning members, children
    // CFO sees: merchant name, amount, category, timestamp
    public const int FullVisibility = 1;

    // Category only — adult earning members (spouse, adult children)
    // CFO sees: category + amount; merchant hashed; Tier2Blurred categories hidden; >₹5,000 surfaced regardless
    public const int CategoryOnly = 2;

    // Aggregate only — financially independent members
    // CFO sees: monthly total only; no line items; alert only if threshold breached
    public const int AggregateOnly = 3;

    // The Rs. 5,000 threshold above which Tier 2 transactions are always surfaced to CFO
    public const decimal Tier2LargeTransactionThreshold = 5000m;

    public static bool IsValid(int tier) => tier is >= 1 and <= 3;
}
