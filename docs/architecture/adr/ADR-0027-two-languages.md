# ADR-0027: The service speaks Ukrainian and English

- Status: accepted (2026-11-08)

## Context

Everything the user read was Ukrainian. The backend and the engine already sent message codes (ADR-0009), but the frontend
had its own words written into templates and code, there was one catalog per owner, and the assistant reading MCP replies
got Ukrainian too. The user should pick the language in the profile, with the browser's language as the default, and an
assistant should always read English.

## Decision

- **Languages are a value in the shared kernel** (`Language`: `uk`, `en`, the default is English). Every catalog exists once
  per language with the same codes: `locales/{uk,en}.json` for the service, `locales/{uk,en}.json` of each strategy in the engine,
  and `frontend/src/i18n/{uk,en}.json` for the screens. Tests fail when a language lacks a code.
- **The frontend uses Transloco** (`@jsverse/transloco` with `@jsverse/transloco-messageformat` for ICU plurals) for its own
  texts, Angular `LOCALE_ID` and locale data for numbers and dates, and the PrimeNG translation for its components. The language
  is chosen once before the app starts: the copy of the profile's choice kept in the browser, otherwise the browser's language
  (Transloco's `getBrowserLang`), otherwise English. Changing it in the profile saves the choice and reloads the page, so data
  already translated by the interceptor is fetched again in the new language.
- **The backend picks the language with ASP.NET Core request localization** from `Accept-Language`, which the frontend sends on
  every API request. Only the UI culture follows the request; the formatting culture stays as it is, so parsing and amounts do
  not change. `MessageLocalizer` renders in the current UI language (PDFs) or in an explicit one.
- **The profile keeps the choice, not a locale** (`language`, empty meaning "as in the browser"). Background texts have no browser:
  Home Assistant notifications use the chosen language, otherwise the language Home Assistant itself is set to.
- **MCP always answers in English**, whatever the request says: the replies are for a language model, not for a person.
- **The engine sends a strategy's texts by language** (`messages: {language: {code: text}}` in the type descriptor); the backend
  stores them that way and serves the ones of the request's language. Its command-line reports stay Ukrainian.
- **Official names stay**: the PIT-38 field names and Polish form words are the same in both languages.

## Consequences

- Message templates keep the ADR-0009 format; `{x:plural:…}` lists the forms of the catalog's language (`one|few|many` in
  Ukrainian, `one|other` in English), and each of the three renderers chooses the form by that language's rule.
- A new language is a catalog per owner, an entry in `Language`, its plural rule in the renderers and its Angular locale data.
- Screen texts are keys in the frontend catalog; ICU syntax applies there (`{name}`, `{count, plural, …}`), so a literal brace or
  a quote before one is escaped.
