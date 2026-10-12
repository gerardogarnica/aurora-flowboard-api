---
paths:
  - "src/Aurora.Flowboard.Application/Authentication/**/*.cs"
  - "src/Aurora.Flowboard.Infrastructure/Authentication/**/*.cs"
  - "src/Aurora.Flowboard.Infrastructure/Configurations/UserTokenConfiguration.cs"
  - "src/Aurora.Flowboard.Domain/Users/**/*.cs"
---

# Authentication and user tokens

## No secrets in `user_tokens`

- The refresh token is stored only as `RefreshTokenHash`: SHA-256, lowercase hex, 64 chars, unique index. Handlers hash the presented value with `ITokenProvider.HashRefreshToken` and look up by hash. The token is 64 random bytes, so no salt or pepper is needed.
- The JWT is never stored; `AccessTokenId` holds its `jti` so access tokens can be revoked later without storing them.
- `ITokenProvider.CreateToken` returns `IssuedToken` (the `IdentityToken` for the client plus the id and the hash to persist). Do not add persistence fields to `IdentityToken`: it is serialized as the login/refresh response body.

## Redeeming a refresh token once

- `UserToken` has a shadow `uint` property `Version` mapped to Postgres' `xmin` (`IsRowVersion()` + explicit `HasColumnName("xmin")`, otherwise EFCore.NamingConventions renames it). EF scaffolds an `AddColumn` for it in the migration, but Npgsql emits no SQL for `xmin`.
- Refresh, logout and change password catch `DbUpdateConcurrencyException` on `SaveChangesAsync`:
  - refresh → `InvalidRefreshToken` (another request redeemed the token first);
  - logout → `Ok` (the session is closed either way);
  - change password → `SessionChangedConcurrently` (409), so the client retries and the retry revokes the rotated token.
- A replayed (already revoked) refresh token is only rejected. It does not revoke the user's other sessions or a token family; `LoggingBehavior` already logs the failure code.

## Load only the tokens you need

Never `.Include(u => u.Tokens)` unfiltered: the history grows with every login and refresh. Use a filtered include — the presented token (`t.RefreshTokenHash == hash`) or the active ones (`!t.IsRevoked && t.RefreshTokenExpiresOnUtc > utcNow`). `MockDbSetHelper` ignores `Include`, so check the SQL log when changing these queries.

## Cleanup

`UserTokenCleanupJob` (Quartz, section `UserTokenCleanup`) deletes in batches the rows whose `RefreshTokenExpiresOnUtc` is older than `RetentionDays`. Revoked tokens expire on the same schedule, so there is no `RevokedOnUtc` column.

## Status codes

- `InvalidCredentials` and `InvalidRefreshToken` are `BaseErrorType.Unauthorized` → 401.
- A wrong current password in change password is `InvalidCurrentPassword` (`Validation`, 400), not 401: the caller is authenticated, and a 401 would make clients with automatic session renewal believe the session expired.
