# Multi-Exchange Provider API & Credential Requirements

[← Back to Documentation Index](../index.md)

## Purpose

Define the mandatory provider public-API verification gate and the per-exchange credential requirement matrix used by account/key setup flows.

This document is the implementation reference for:

- `Services/ExchangeProvider.cs`
- `Services/ExchangeProviderAuditService.cs`
- `Services/ExchangeCredentialPolicy.cs`
- `UI/AccountEditDialog.cs`
- `UI/KeyEditDialog.cs`

## Provider Public API Verification Gate

Before strategy/routing certification, each mandatory venue must pass provider-level public API checks:

- Public client creation via provider factory.
- Product discovery (`ListProductsAsync`) returns non-empty market list.
- Ticker probe (`GetTickerAsync`) returns valid last price on a discovered symbol.
- Latency capture for discovery + ticker probes.
- Deterministic PASS/FAIL artifact persisted for each run.

Primary implementation surface:

- `Services/ExchangeProviderAuditService.cs` (`ValidatePublicApisAsync`).

## Credential Requirement Matrix (Enforced)

| Service | Required fields |
| --- | --- |
| `coinbase-advanced` | API Key Name + EC Private Key (PEM) |
| `coinbase-exchange` | API Key + API Secret + Passphrase |
| `binance` | API Key + API Secret |
| `bybit` | API Key + API Secret |
| `okx` | API Key + API Secret + Passphrase |
| `kraken` | API Key + API Secret |
| `bitstamp` | API Key + API Secret |
| `paper` | None |

Primary implementation surface:

- `Services/ExchangeCredentialPolicy.cs` (`ExchangeCredentialPolicy.ForService`).
- `UI/AccountEditDialog.cs` (save gating + field visibility).
- `UI/KeyEditDialog.cs` (save validation).

## Validation Rules

- Missing required credential fields are hard-blocked with deterministic validation messages.
- Existing saved keys can be selected without re-entry when no new edits are present.
- If edits are present, required fields must satisfy the selected service policy before save.
- Paper service bypasses API credential requirements.

## Research References

Credential requirements were validated against exchange auth docs used by current adapters:

- Binance Spot API Request Security (`X-MBX-APIKEY`, signed timestamp payload).
- Bybit V5 Integration Guidance (`X-BAPI-API-KEY`, `X-BAPI-SIGN`, `X-BAPI-TIMESTAMP`, `X-BAPI-RECV-WINDOW`).
- Coinbase Exchange REST auth (`cb-access-key`, `cb-access-passphrase`, `cb-access-sign`, `cb-access-timestamp`).
- Kraken Spot REST auth (`API-Key`, `API-Sign`, nonce). 
- Bitstamp API v2 auth (`X-Auth`, `X-Auth-Signature`, `X-Auth-Nonce`, `X-Auth-Timestamp`, `X-Auth-Version`).

For OKX, current adapter implementation enforces key/secret/passphrase headers and must remain aligned with official OKX v5 auth guidance during certification updates.
