# ADR-0021: A found fund is registered by its trading symbol

- Status: accepted (2026-11-03)
- Complements: ADR-0008

## Context
The Strategy Builder finds ETFs at the quote provider, but the search result does not contain an ISIN.
Ordinary manual registration of an ETF requires an ISIN. Without a separate way of registration, all newly found funds would be rejected.

## Decision
- `Instrument.FromFundListing` creates an ETF with a mandatory trading symbol, name, exchange and currency.
  The ISIN may be unknown. The ordinary `Instrument.Register` keeps its ISIN check.
- `RegisterListedFund` reuses the owner of a known symbol. The instrument registry routes a found ETF to this action, after which it calls the existing retrieval of the description.
- The symbol format and the exchange detection stay in MarketData. Exchange names that exceeded the length of the column were replaced by their short MIC codes in the symbol convention.
- The search does not guess a missing currency, asset class or investment region.

## Consequences
An ETF without an ISIN can take part in a strategy by its trading symbol. The symbol uniqueness check and the existing uniqueness check of a known ISIN remain in force. Different trading symbols of one fund without an ISIN are not yet merged automatically.

The existing DescribeInstruments retrieves the description of an instrument, but does not determine the asset class and the investment region. The search plan wrongly assumed that this classification already existed. The new search calls the existing description retrieval; missing exposure data remains unknown. Automatic classification requires a separate implementation.
