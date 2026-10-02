# swiftbets-cashout

Cashout for SwiftBets singles and accumulators. Stateless: a quote is a signed token, not a database row.

- `POST /cashout/quote` `{ couponId }` prices the coupon at live odds: what has won stays won, each open leg is bought back at placed ÷ live odds, less a margin, capped at the potential payout. The reply carries a quote token (HMAC-SHA256) valid for 10 seconds.
- `POST /cashout/execute` `{ quoteToken }` verifies the token and its age, reprices at live odds, and pays the quoted amount if the value has not fallen by more than the leeway (2%). Otherwise it refuses with `409` and a fresh quote. Settlement's final-state lock does the paying (gRPC `swiftbets.settlement.cashout.v1`); the quote id is the cashout id, so a retried execute pays once.

Configuration: `Cashout:SigningKey` (base64, 32+ bytes, from the environment or a secret store), `Cashout:Margin`, `Cashout:Leeway`, `Cashout:QuoteMaxAgeSeconds`; `Clients:SettlementGrpcAddress`, `Clients:OfferAddress`; `ServiceIdentity:*` for the service token settlement requires.
