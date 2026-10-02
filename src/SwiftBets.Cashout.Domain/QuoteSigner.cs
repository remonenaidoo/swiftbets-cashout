using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SwiftBets.Cashout.Domain;

/// <summary>
/// Quotes are stateless: the token carries the quote and an HMAC-SHA256 over it, so execute can trust the amount
/// without storing anything. Verification is constant-time and rejects a token older than its max age.
/// </summary>
public sealed class QuoteSigner(byte[] key, TimeSpan maxAge)
{
    public string Sign(CashoutQuote quote)
    {
        ArgumentNullException.ThrowIfNull(quote);
        var payload = Payload(quote);
        return $"{Encode(Encoding.UTF8.GetBytes(payload))}.{Encode(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(payload)))}";
    }

    public QuoteCheck Verify(string token, DateTimeOffset now)
    {
        var parts = (token ?? string.Empty).Split('.');
        if (parts.Length != 2 || !TryDecode(parts[0], out var payloadBytes) || !TryDecode(parts[1], out var signature))
        {
            return QuoteCheck.Invalid;
        }

        if (!CryptographicOperations.FixedTimeEquals(HMACSHA256.HashData(key, payloadBytes), signature))
        {
            return QuoteCheck.Invalid;
        }

        var quote = Parse(Encoding.UTF8.GetString(payloadBytes));
        if (quote is null)
        {
            return QuoteCheck.Invalid;
        }

        return now - quote.IssuedAt > maxAge || quote.IssuedAt > now.AddSeconds(5) ? QuoteCheck.Expired(quote) : QuoteCheck.Valid(quote);
    }

    private static string Payload(CashoutQuote q) => string.Join('|', "v1", q.QuoteId.ToString("N"), q.CouponId.ToString("N"), q.PunterId.ToString("N"),
        q.Amount.ToString(CultureInfo.InvariantCulture), q.Currency, q.IssuedAt.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture));

    private static CashoutQuote? Parse(string payload)
    {
        var f = payload.Split('|');
        return f.Length == 7 && f[0] == "v1"
            && Guid.TryParseExact(f[1], "N", out var quoteId) && Guid.TryParseExact(f[2], "N", out var couponId) && Guid.TryParseExact(f[3], "N", out var punterId)
            && long.TryParse(f[4], NumberStyles.None, CultureInfo.InvariantCulture, out var amount)
            && long.TryParse(f[6], NumberStyles.None, CultureInfo.InvariantCulture, out var issuedMs)
            ? new CashoutQuote(quoteId, couponId, punterId, amount, f[5], DateTimeOffset.FromUnixTimeMilliseconds(issuedMs))
            : null;
    }

    private static string Encode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static bool TryDecode(string text, out byte[] bytes)
    {
        var padded = text.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - (padded.Length % 4)) % 4), '=');
        bytes = new byte[padded.Length];
        return Convert.TryFromBase64String(padded, bytes, out var written) && (bytes = bytes[..written]).Length > 0;
    }
}
