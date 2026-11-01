# Frontend (Angular 21, PrimeNG 21, Tailwind)

## Commands (from `frontend/`)

```bash
npx ng build                 # the check to run after every change (type errors in templates show up here)
npm start                    # dev server on :4200; /api is proxied to the API (proxy.conf.js)
npx playwright test          # e2e against a running stack (start the Aspire AppHost first; see playwright.config.ts)
```

Normally the whole stack, frontend included, is started by the Aspire AppHost in `../backend`. There are no unit
specs yet; verify UI changes by building and, when a stack is running, in the browser or with Playwright.

## Layout

- `src/app/core/` — API client, auth, account context, layout, notifications, theme.
- `src/app/shared/` — reusable UI (`shared/ui`), pipes (`money`, `date`, `dateTime`, `period`… in `shared/pipes`),
  charts, models.
- `src/app/features/<feature>/` — `pages/` (routed), `ui/` (presentational components), `data-access/` (`*-api.ts`
  HTTP calls, `*-store.ts` state, `*-mapper.ts` DTO → view model), `models/` (`*-dto.ts` mirrors the API,
  `*-vm.ts` what templates bind to), `<feature>.routes.ts` (lazy).

## Conventions

- Standalone components, `ChangeDetectionStrategy.OnPush`, signals everywhere: `input()`/`input.required()`,
  `computed`, `resource()` for loading; new control flow (`@if`, `@for … track`). No NgModules, no `*ngIf`.
- Stores are `@Injectable()` provided by their page; they expose read-only signals and async command methods that
  report results via PrimeNG `MessageService`.
- PrimeNG components for controls, Tailwind/PrimeUI utility classes for layout (`card`, `text-muted-color`…).
  Check phone layout: pages switch on `Viewport.phone()`.
- A page below a list gives `app-page-header` its parent (`backLink`, `backLabel`); «back» itself follows where the
  user came from (`core/layout/navigation-history.ts`), so a programmatic step that should not be returned to uses `replaceUrl`.
- All user-facing text is Ukrainian; dates as `dd.MM.yyyy`, money through the `money` pipe.
- When the API changes, update the DTO in `models/*-dto.ts` and the mapper together.
- No business logic here: the backend decides and calculates, and the frontend formats what it receives in mappers and
  shows it. No tax rates, policy codes, provider names or symbol formats (see Modularity in `../CLAUDE.md`).
