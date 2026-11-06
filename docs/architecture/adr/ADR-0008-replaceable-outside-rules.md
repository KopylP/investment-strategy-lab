# ADR-0008: The quote provider, exchange rates and tax rules each have one owner

- Status: accepted (2026-11-01)
- Context: [ADR-0004](ADR-0004-python-strategy-engine.md), [ADR-0009](ADR-0009-messages-as-codes.md)

## Context
The `yfinance:` prefix was parsed by eight handlers in six modules and by the frontend, and exchange suffixes were in statement import. The rule "everything through PLN and NBP" had been copied into four modules. The tax policy had blocks, but the currency, the rate, the texts and even the list of account types lay outside it, and an unknown policy code silently became Belka and produced plausible but wrong numbers.

## Decision
- **`InstrumentDto.Ticker`, `Label`, `IsVolatilityIndex`** are a neutral description of an instrument. Strategies refer to ISINs by default.
- **`QuoteProvider.Primary` and `ISymbolConvention`** in the MarketData domain are the single place for the provider's rules.
- **`PrimaryFxSource` and `IFxRates.GetCrossRate`** are the single place for the source of exchange rates and the base currency; modules ask for a cross rate.
- **`ITaxPolicy` decides:** `IFxRule` (currency, source and day of the rate), `ITaxRateSchedule.TaxOn` (the scale, including the tax-free minimum), `ITaxableGainRule` (what part of the gain is taxed, including the relief for the holding period), `DividendRate`, `CarriesLosses`, `TaxPolicyTexts`.
- **The frontend asks:** `GET /api/taxation/regime` gives the currency and the texts, `GET /api/taxation/policies` gives the list of account types.
- **An unknown policy code is an error:** refusal when opening an account, and an exception in `TaxPolicies.For`.
- **The effect of a sale on tax** is computed by a pure domain function as the difference of two full calculations, so it works with any policy.
- **Tax amounts in types are named `…InTaxCurrency`**, not `…Pln`.
- `ProviderNeutralityTests` fails when the name of a provider or of an exchange-rate source appears outside its owner.

## Consequences
- A new quote provider is a new adapter and convention. A new exchange-rate source is a new `IFxRateProvider`. A new tax policy is a new class and a line in `TaxPolicies.All`.
- The following stay in Polish on purpose: the PIT-38 form, its screens and printing, because a declaration belongs to the country by nature, and the reading of the Polish Revolut P&L statement.
- `TaxAssessmentUpdatedV2.TaxDuePln` was not renamed, because published event versions do not change.
- Stored broker statements keep the `…Pln` keys in JSON through `JsonPropertyName`: migrating this data would bring no benefit.
- Symbols in instrument seeds are written in the format of the current provider: for a new provider, its symbols are added to the instruments.
- The engine reads the market through `market_data.py` and the strategy's own columns `SPY` and `^VIX`; replacing the price source inside the engine touches this module and the strategy.
