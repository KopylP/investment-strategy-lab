import { expect, test } from '@playwright/test';

test('trial comparison and publication preserve the draft lifecycle', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', (error) => errors.push(error.message));
  await page.goto('/auth/register');
  await page.locator('#email').fill(`e2e-publish-${Date.now()}@example.com`);
  await page.locator('#password').fill('correct horse battery');
  await page.locator('#confirm').fill('correct horse battery');
  await page.getByRole('button', { name: 'Зареєструватися' }).click();
  await expect(page.getByRole('heading', { name: 'Огляд', exact: true })).toBeVisible();
  await page.goto('/strategies');
  await page.getByRole('button', { name: 'Нова власна стратегія' }).click();
  await expect(page.getByRole('dialog')).toBeVisible();
  const created = await page.request.post('/api/strategy-builder/drafts', {
    data: { name: 'Публікація з конструктора', template: 'momentum-trend-filter' },
  });
  expect(created.ok()).toBeTruthy();
  let draft = await created.json();
  const instruments = await (await page.request.get('/api/strategy-builder/instruments')).json();
  const regular = instruments.filter(
    (instrument: { isVolatilityIndex: boolean }) => !instrument.isVolatilityIndex,
  );
  const configured = await page.request.put(`/api/strategy-builder/drafts/${draft.id}`, {
    data: {
      revision: draft.revision,
      setup: {
        funds: regular.slice(0, 5).map((instrument: { id: string }) => instrument.id),
        benchmarkId: regular[0].id,
        cashId: regular[5].id,
        volatilityIndexId: null,
        extraSeries: [],
      },
    },
  });
  expect(configured.ok()).toBeTruthy();
  draft = await configured.json();
  await page.goto(`/strategy-builder/${draft.id}`);
  await page.getByRole('button', { name: 'Бектест', exact: true }).click();
  await expect(page).toHaveURL(new RegExp(`${draft.id}/trials`));
  const runs = await (
    await page.request.get(`/api/strategy-builder/drafts/${draft.id}/trials`)
  ).json();
  expect(runs).toHaveLength(1);
  await page.goto(`/strategy-builder/${draft.id}`);
  await page.getByRole('button', { name: 'Бектест', exact: true }).click();
  await expect(page).toHaveURL(new RegExp(`${draft.id}/trials`));
  expect(
    await (await page.request.get(`/api/strategy-builder/drafts/${draft.id}/trials`)).json(),
  ).toHaveLength(1);

  const summary = {
    totalReturn: 0.2,
    annualReturn: 0.1,
    annualReturnAfterTax: 0.08,
    volatility: 0.12,
    sharpe: 0.8,
    maxDrawdown: -0.15,
  };
  let reads = 0;
  await page.route(`**/api/strategy-builder/drafts/${draft.id}/trials`, (route) => {
    reads++;
    const first = { ...runs[0], ...summary, status: reads === 1 ? 'Running' : 'Succeeded' };
    return route.fulfill({
      json: [
        first,
        {
          ...first,
          number: 2,
          version: 2,
          status: 'Succeeded',
          annualReturn: 0.15,
          label: 'Два фонди',
        },
        {
          ...first,
          number: 3,
          version: 3,
          status: 'Failed',
          failureReason: 'Недостатньо історії для бектесту',
        },
      ],
    });
  });
  await page.route(`**/api/backtesting/strategies/${runs[0].strategyId}?*`, (route) => {
    const second = new URL(route.request().url()).searchParams.get('version') === '2';
    const points = (
      second ? ['2025-02-01', '2025-03-01'] : ['2025-01-01', '2025-02-01', '2025-03-01']
    ).map((date, index) => ({ date, growth: 1 + index / 10, worstDrawdown: -0.05 }));
    return route.fulfill({
      json: {
        status: 'Succeeded',
        result: {
          start: points[0].date,
          end: points.at(-1)!.date,
          currency: 'EUR',
          trades: 1,
          riskExits: 0,
          signalReports: 0,
          series: [
            { code: 'strategy', label: 'Стратегія', summary, points },
            { code: 'benchmark', label: 'Бенчмарк', summary, points },
          ],
        },
      },
    });
  });
  await page.route('**/money-outcomes?*', (route) =>
    route.fulfill({ json: { contributed: 0, years: 5, series: [] } }),
  );
  await page.reload();
  await expect(page.getByText('Недостатньо історії для бектесту', { exact: true })).toBeVisible();
  await expect(page.getByRole('checkbox', { name: 'Вибрати запуск 3' })).toBeDisabled();
  await expect.poll(() => reads, { timeout: 15_000 }).toBeGreaterThan(1);
  await page.getByRole('checkbox', { name: 'Вибрати запуск 2' }).check();
  const comparison = page.locator('app-trial-comparison');
  await expect(comparison).toBeVisible();
  await expect(comparison.getByRole('button', { name: 'Запуск B · №2' })).toBeVisible();
  await expect(comparison.getByRole('button', { name: 'Бенчмарк' })).toBeVisible();
  const line = comparison.getByRole('button', { name: 'Запуск A · №1' });
  await line.click();
  await expect(line).toHaveAttribute('aria-pressed', 'false');
  await expect(comparison.locator('canvas')).toBeVisible();
  await page.screenshot({ path: 'test-results/builder-trial-comparison.png', fullPage: true });
  await page.setViewportSize({ width: 390, height: 844 });
  await page.reload();
  await page.getByRole('checkbox', { name: 'Вибрати запуск 2' }).check();
  await expect(comparison).toBeVisible();
  await expect
    .poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth))
    .toBeTruthy();
  await page.screenshot({ path: 'test-results/builder-trials-phone.png', fullPage: true });
  await page.setViewportSize({ width: 1440, height: 900 });

  await page.goto(`/strategy-builder/${draft.id}/trials?run=3`);
  await expect(page.getByRole('checkbox', { name: 'Вибрати запуск 3' })).not.toBeChecked();
  await page.getByRole('checkbox', { name: 'Вибрати запуск 1' }).check();
  await page.getByRole('checkbox', { name: 'Вибрати запуск 2' }).check();
  await expect(comparison).toBeVisible();

  await page.goto(`/strategy-builder/${draft.id}`);
  await page.getByRole('button', { name: 'Опублікувати', exact: true }).click();
  await expect(page.getByRole('alertdialog', { name: 'Публікація стратегії' })).toContainText(
    'Опублікувати як нову стратегію',
  );
  await page
    .getByRole('alertdialog', { name: 'Публікація стратегії' })
    .getByRole('button', { name: 'Опублікувати', exact: true })
    .click();
  await expect(page).toHaveURL(/\/strategies\/[\w-]+$/);
  const publishedId = page.url().split('/').at(-1)!;
  await expect(page.getByRole('heading', { name: 'Стратегія словами' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Параметри', exact: true })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Змінити', exact: true })).toHaveCount(0);
  await expect(page.locator('app-builder-words-panel')).not.toContainText('@');
  await page.screenshot({ path: 'test-results/builder-published.png', fullPage: true });
  expect((await page.request.get(`/api/strategy-builder/drafts/${draft.id}`)).status()).toBe(404);
  await page.getByRole('button', { name: 'Змінити в конструкторі' }).click();
  await expect(page).toHaveURL(/\/strategy-builder\/[\w-]+$/);
  const reopenedId = page.url().split('/').at(-1)!;
  const reopened = await (
    await page.request.get(`/api/strategy-builder/drafts/${reopenedId}`)
  ).json();
  expect(reopened.graph).toEqual(draft.graph);
  await page.getByRole('button', { name: 'Опублікувати', exact: true }).click();
  await expect(page.getByRole('alertdialog', { name: 'Публікація стратегії' })).toContainText(
    'Опублікувати версію 2',
  );
  await page
    .getByRole('alertdialog', { name: 'Публікація стратегії' })
    .getByRole('button', { name: 'Опублікувати', exact: true })
    .click();
  await expect(page).toHaveURL(new RegExp(`/strategies/${publishedId}$`));
  await expect(page.getByRole('combobox').first()).toContainText('Версія 2');
  const duplicate = await page.request.post('/api/strategy-builder/drafts', {
    data: { name: draft.name, template: 'momentum-trend-filter' },
  });
  const duplicateDraft = await duplicate.json();
  await page.request.put(`/api/strategy-builder/drafts/${duplicateDraft.id}`, {
    data: { revision: duplicateDraft.revision, setup: draft.setup },
  });
  await page.goto(`/strategy-builder/${duplicateDraft.id}`);
  await page.getByRole('button', { name: 'Опублікувати', exact: true }).click();
  await page
    .getByRole('alertdialog', { name: 'Публікація стратегії' })
    .getByRole('button', { name: 'Опублікувати', exact: true })
    .click();
  await expect(page.locator('#draft-name-error')).toBeVisible();
  expect(
    (await page.request.get(`/api/strategy-builder/drafts/${duplicateDraft.id}`)).ok(),
  ).toBeTruthy();
  expect(errors).toEqual([]);
});
