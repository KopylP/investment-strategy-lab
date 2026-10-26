# PIT-38 generator — review brief

For an agent reviewing commits `57a103c` (PIT-38, PIT/ZG, fee as a cost) on branch `docs/dotnet-migration-plan`.
Goal of the review: **no wrong number may reach a tax return.** Check every rule below against its source, then the
code and the test that pins it. Report a rule that is wrong, missing or untested, with the source that says so.

## What was built

- `ITaxDeclarationForm` (`backend/src/Modules/Taxation/InvestmentLab.Taxation/Declarations/Domain/TaxDeclaration.cs`):
  optional yearly returns, chosen by the account's tax policy's `Jurisdiction` (`ITaxPolicy.Jurisdiction`, `PL` for
  `PL-BELKA`, `PL-IKE`, `PL-IKZE`). A new country = a new policy + a new form registered in `TaxationModule`.
- `Pit38Form` (`Declarations/Domain/Pit38.cs`): PIT-38 of the **person** for a year, from all their accounts.
- `DeclarationQueries` + `GET /api/taxation/declarations/{year}` (forms available) and
  `GET /api/taxation/declarations/{year}/{code}` (filled form + warnings).
- `TaxCalculator` change: the sale's fee is a **cost** (was taken off revenue). Gain unchanged; `BelkaTaxPolicy.Version` 2 → 3
  so every stored assessment is recomputed once.
- UI: `frontend/src/app/features/taxes/ui/declaration-dialog/` — field by field with "poz." numbers, copy buttons.

Tests: `backend/tests/Modules/InvestmentLab.Taxation.Tests/Pit38FormTests.cs` (form rules),
`TaxCalculatorTests.cs` (rounding, fees, lots, loss carryforward, dividend credit),
`backend/tests/InvestmentLab.IntegrationTests/Taxation/TaxationScenarioTests.cs` (endpoint end to end).
Run: `dotnet test --project backend/tests/Modules/InvestmentLab.Taxation.Tests` (57 pass) and
`dotnet test --project backend/tests/InvestmentLab.IntegrationTests --filter-class "*TaxationScenarioTests"` (Docker).

## Sources

| # | Source | Used for |
|---|--------|----------|
| S1 | MF, *Broszura informacyjna do zeznania PIT-38 za 2025* — https://www.podatki.gov.pl/media/g5ebnm2e/broszura-do-pit-38-za-2025-r.pdf | every position, rounding, FX rule, loss rule, PIT/ZG scope |
| S2 | MF, form **PIT-38(18)** — https://www.gov.pl/attachment/1874d956-4063-4a9c-a65c-ab7b6bc7aa30 (listed on https://www.podatki.gov.pl/podatki-osobiste/pit/formularze) | position numbers and their formulas, footnote 8 (rounding of art. 30a tax) |
| S3 | MF, form **PIT/ZG(8)** — https://www.gov.pl/attachment/213803ec-f7e6-4b15-b643-e25dc145e2bd (page https://www.gov.pl/web/finanse/pitzg, valid for 2023–2026) | PIT/ZG poz. 6, 7, 29, 30; one per country |
| S4 | podatekgieldowy.pl, *Rozliczenie PIT-38 za 2025* — https://www.podatekgieldowy.pl/rozliczenie-pit-38-2026 | cross-check of positions C.20–29, D.30–35, G.46–52 |
| S5 | stockbroker.pl, *Kiedy i jak wypełnić PIT/ZG* — https://stockbroker.pl/kiedy-i-jak-wypelnic-pit-zg-2026/ | PIT/ZG practice: dividends not in it, country choice, e-Urząd asks for it |
| S6 | prawo.pl, *Odliczenie podatku zapłaconego za granicą – wyrok NSA* — https://www.prawo.pl/podatki/odliczenie-podatku-zaplaconego-za-granica-wyrok-nsa,521976.html; KIP — https://izbapodatkowa.pl/baza-wiedzy/odliczenie-podatku-od-dywidend-zaplaconego-w-panstwie-zrodla/ | art. 30a ust. 9: NSA caps the credit at 19%, KIS at the treaty rate |
| S7 | SII, *metoda FIFO* — https://www.sii.org.pl/6568/podatek-gieldowy/wszystko-o-podatku-gieldowym/wedlug-jakiej-metody-ksiegowej-identyfikowane-sa-koszty-podatkowe-co-to-jest-metoda-fifo.html | art. 24 ust. 10: FIFO per account when lots are not identified |
| S8 | pit.pl, *Jak rozliczyć transakcje z Revolut w PIT za 2025* — https://www.pit.pl/aktualnosci/sprawdz-jak-rozliczyc-transakcje-z-revolut-w-pit-za-2025-rok | Revolut sends no PIT-8C → own PIT-38, row 2 |

## Rules to verify

| Rule | Source | Code | Test |
|------|--------|------|------|
| Amounts in PLN at the NBP average rate of the last business day before the revenue/cost/payment (art. 11a) | S1 p.1 | `TaxInputs.RateAsync` (unchanged) | integration test (`FxRateDate < trade day`) |
| Revenue = price × quantity; costs = purchase price + **both** fees (buy and sell) | S1 p.4 (poz. 23) | `TaxCalculator.Sell` | `Both_fees_are_costs_each_at_the_rate_of_its_own_trade`, `Fifo_takes_the_oldest_lots…` |
| FIFO, per account | S7 | `FifoLotMatcher`, one `TaxCalculator.Compute` per account | `Fifo_takes_the_oldest_lots…`, `A_partly_sold_lot…`, `Each_instrument_has_its_own_lots` |
| Row 1 (poz. 20/21) = PIT-8C brokers, row 2 (poz. 22/23) = others incl. foreign | S1 p.4, S2 footnotes 3–4, S8 | `Pit38Form.Fill`, `PolishBrokers` | `Brokers_with_PIT_8C_go_to_row_1…`, `Polish_brokers_are_recognised_by_whole_words` |
| poz. 26 = 20+22−24, 27 = 21+23−25, 28 = income, 29 = loss | S1 p.5, S2 | `Pit38Form.Fill` | `Brokers_with_PIT_8C…`, `A_year_with_a_loss…` |
| Losses: next 5 years, ≤ 50 % of the loss per year, poz. 30 ≤ poz. 28; per **person**, not per account | S1 p.6 (art. 9 ust. 3) | `Pit38Form.CarryforwardUsed`, `LossPool.UseAgainst`, `Pl5Years50Percent` | `Earlier_losses_are_the_persons_netted…`, `A_loss_of_one_account…`, `Losses_entered_by_the_user…`, `A_loss_is_usable_in_its_fifth_year…`, `The_deduction_never_exceeds…`, `What_earlier_years_deducted…`, `A_later_years_loss…` |
| poz. 31 whole złoty (< 50 gr down, ≥ 50 gr up); poz. 33 with grosze; poz. 35 whole złoty | S1 p.6, S2 | `PlWholeZloty`, `Pit38Form.Fill` | `The_base_is_whole_zloty…` (theory), `Tax_base_and_tax_are_whole_zloty…` |
| poz. 34 = 0 (no foreign tax on gains) | S1 p.6 | `Pit38Form.Fill` | `Brokers_with_PIT_8C…` |
| Part G: poz. 47 = 19 % of gross foreign dividends (grosze), poz. 48 = tax paid abroad ≤ poz. 47, poz. 49 = 47 − 48 whole złoty (dividends are art. 30a ust. 1 pkt 4, so the "round grosze up" exception for pkt 1–3 does not apply) | S1 p.8, S2 footnote 8 | `Pit38Form.Fill`, `TaxCalculator.Compute` (per-dividend 19 % cap) | `Foreign_dividends_are_taxed_in_part_G…`, `Poz_47_keeps_grosze…`, `A_dividend_nothing_was_withheld…`, `Tax_paid_abroad_on_a_dividend_is_credited_only_up_to…` |
| Dividends paid through a Polish payer (XTB) are not in part G | S1 p.8 (poz. 46/47 only for tax the payer did not take) | `Pit38Form.Fill` (row 2 only) | `Foreign_dividends_are_taxed_in_part_G…` |
| A capital loss does not reduce dividend tax | S2 (sections D and G are separate) | `Pit38Form.Fill` | `A_capital_loss_does_not_reduce_the_dividend_tax`, `Dividend_tax_is_rounded_once…` |
| poz. 51 = 35 + 45 + 46 + 49 − 50 | S2 | `Pit38Form.Fill` | several |
| PIT/ZG: one per country, poz. 29 income, poz. 30 tax paid abroad (0); dividends not in it; poz. 72 = count | S1 p.10, S3, S5 | `Pit38Form.Fill` (attachments) | `PIT_ZG_is_one_per_country…`, `Polish_papers_and_sales_reported_on_PIT_8C…`, `A_sale_without_an_isin…` |
| IKE/IKZE not declared | — | `NoTaxPolicy`, `Pit38Form.Fill` | `IKE_accounts_are_left_out_and_named` |
| Deadline 30 April of the next year | S1 p.1, S2 | `Pit38Form.Fill` | `The_return_is_due_on_30_April…` |

## Decisions the reviewer should challenge

1. **Foreign dividend tax over the treaty rate (no W-8BEN, 30 % US):** credited up to **19 %** (NSA, S6). KIS allows only
   the treaty rate. The form keeps the NSA figure and adds a note with the KIS figures, assuming a **15 %** treaty rate
   (US, DE, FR, CH, NL…). Countries with a 10 % treaty rate (e.g. Japan) are not handled.
2. **PIT/ZG when nothing was withheld abroad:** S1 ties PIT/ZG to the credit method; S5 says e-Urząd asks for it anyway.
   It is generated for countries with a positive net gain only; the note says this is disputed. Country = the ISIN's
   prefix (one of three readings in S5); `PL` ISINs are skipped.
3. **Row 1 uses our own numbers**, not the broker's PIT-8C. The note tells the user to enter the PIT-8C figures.
4. **XTB and other Polish brokers are assumed to be the payer of dividend tax** and their dividends are left out of part G.
   Verify for XTB's foreign dividends.
5. **Loss carryforward models only the "≤ 50 % a year" option.** The one-time "up to 5 000 000 zł" option (art. 9 ust. 3
   pkt 2) is only mentioned in a note.
6. **The per-account tax page still nets losses per account**, while PIT-38 nets per person. The two differ when one
   account loses and another gains. The page is an estimate; the return is authoritative.
7. **Losses entered by hand are deduplicated by value** across accounts: the same `(year, amount, used)` twice counts once.
8. **`SaleOfIncome` (spin-off income the CSV recorded, re-read as a sale)** still uses the journal's net income as
   revenue, so its fee is netted, not shown as a cost. Gain is right, poz. 22/23 are off by that fee.
9. **Year ≥ 2026:** the 2026 form is not published yet; positions follow PIT-38(18), and a note says so.
10. **Crypto (parts E/F) is not handled**; the app has no crypto.

## Out of scope

Personal data (parts A, B), 1.5 % OPP (part J), bank account (part M), e-Deklaracje XML.

## Review 1 (agent, 2026-10-26) — what changed

| # | Finding | Outcome |
|---|---------|---------|
| 1 | Foreign dividends of XTB/mBank fell out of part G | **Fixed.** Part G takes every foreign dividend of every taxed account (XTB guide §21, mBank 2026 guide); only ISIN `PL` stays out, with a note to check it was taxed when held abroad. |
| 2 | Dividend without the broker's report = gross = net, silently | **Fixed as a warning.** `DividendIncome.WithholdingKnown`; the form names such dividends and says poz. 47/48 assume nothing was withheld. |
| 3 | NBP rate from the trade day, not the settlement day (art. 17 ust. 1ab) | **Open — the user decides.** A note states which date is used and the other reading. |
| 4 | Losses counted twice | **Fixed.** One entered loss per year (two different ones are an error); an entered year stands instead of the journal's computed loss; "already used" = used in returns for years outside the journal (UI says so). |
| 5 | Incomplete data still filled the form | **Fixed.** Missing NBP rates or sold units without a purchase return `tax.declaration_incomplete` with the reasons. |
| 6 | Row 1 from our numbers | **Fixed.** PIT-8C poz. 35/36 per account and year (`pit8c_statements`), row 1 and everything below follow them. |
| 7 | `SaleOfIncome` revenue net of the fee | Open (low): the fee is not known for that entry. |
| 8 | PL dividends on a foreign broker | Note added (see 1). |
| 9 | Deadline on a weekend/holiday | **Fixed** (`Pit38Form.Deadline`, art. 12 § 5 OP). |
| 10 | Banker's rounding; half of a loss | **Fixed:** away from zero everywhere; half of a loss rounds down to the grosz. Policy v4. |
| 11 | poz. 47 base to whole złoty? | Open — no MF position found. |
| 12 | Account fees, foreign interest | Open (not in the journal). |
| 13 | Gap years offered, PIT/ZG with poz. 28 = 0, XS/EU as countries | **Fixed.** |
| D1 | KIS treaty rate 15% wrong for JP/IT | **Fixed:** 10% for JP and IT. |
| D10 | Crypto | Note added. |

PDF: `Pit38PdfPrinter` writes the figures on the official PIT-38(18) and PIT/ZG(8) (measured boxes, read back in `Pit38PdfPrinterTests`).
