import { expect, test } from '@playwright/test';
import { mockStrategyWorkspace } from './support/strategy-workspace';

test('trial rows select by any cell or keyboard and switch between a running run and comparisons', async ({
  page,
}, testInfo) => {
  await mockStrategyWorkspace(page);
  const base = {
    strategyId: 'demo',
    version: 1,
    label: null,
    ranAt: '2026-11-05T18:25:00Z',
    failureReason: null,
    annualReturn: 0.1,
    annualReturnAfterTax: 0.08,
    volatility: 0.2,
    sharpe: 0.5,
    maxDrawdown: -0.3,
  };
  await page.route('**/api/strategy-builder/drafts/workspace/trials', (route) =>
    route.fulfill({
      json: [
        { ...base, number: 1, status: 'Succeeded' },
        { ...base, number: 2, status: 'Running' },
        { ...base, number: 3, version: 3, status: 'Succeeded' },
        { ...base, number: 4, status: 'Failed', failureReason: 'Недостатньо історії' },
      ],
    }),
  );
  await page.route('**/api/backtesting/**', (route) =>
    route.fulfill({ json: { status: 'Succeeded', result: null } }),
  );
  await page.goto('/strategy-builder/workspace/trials?run=2');
  const row = (number: number) => page.locator(`[data-trial-number="${number}"]`);
  await expect(row(2)).toHaveAttribute('aria-selected', 'true');
  await expect(page.locator('input[type=checkbox]')).toHaveCount(0);
  await row(1).locator('td').nth(2).click();
  await expect(row(1)).toHaveAttribute('aria-selected', 'true');
  await expect(row(2)).toHaveAttribute('aria-selected', 'false');
  await row(3).locator('td').nth(4).click();
  await expect(row(3)).toHaveAttribute('aria-selected', 'true');
  await expect(page.locator('app-trial-comparison')).toBeVisible();
  await page.screenshot({ path: testInfo.outputPath('trial-row-comparison.png') });
  await row(3).focus();
  await row(3).press('Space');
  await expect(row(3)).toHaveAttribute('aria-selected', 'false');
  await row(2).locator('td').first().click();
  await expect(row(1)).toHaveAttribute('aria-selected', 'false');
  await expect(row(2)).toHaveAttribute('aria-selected', 'true');
  await row(4).click({ force: true });
  await expect(row(4)).toHaveAttribute('aria-selected', 'false');
  await expect(row(2)).toHaveAttribute('aria-selected', 'true');
  await row(3).focus();
  await row(3).press('Enter');
  await expect(row(3)).toHaveAttribute('aria-selected', 'true');
  await expect(row(2)).toHaveAttribute('aria-selected', 'false');
});

test('risk limits explain empty fields and node menus match the visual control width', async ({
  page,
}, testInfo) => {
  const fixture = await mockStrategyWorkspace(page, (draft, vocabulary) => {
    vocabulary.blockFields = Object.fromEntries(
      ['stopLossFromCost', 'drawdownFromPeak'].map((name) => [
        name,
        {
          name,
          kind: 'Fraction',
          label: name,
          required: false,
          minimum: -1,
          maximum: 0,
          default: null,
          choiceLabels: {},
        },
      ]),
    );
    vocabulary.conditions
      .find((condition: any) => condition.code === 'compare')
      .fields.unshift({
        name: 'operator',
        label: 'Як',
        kind: 'Choice',
        required: true,
        default: 'gt',
        choiceLabels: { gt: 'більше за', lt: 'менше за', gte: 'не менше за', lte: 'не більше за' },
      });
    draft.graph.nodes.find((node: any) => node.id === 'compare').parameters.operator = 'gt';
  });
  await page.setViewportSize({ width: 2560, height: 1440 });
  await page.goto('/strategy-builder/workspace');
  await expect(page.locator('app-graph-editor .palette')).toHaveCSS('width', '260px');
  await page
    .getByRole('navigation', { name: 'Ролі стратегії' })
    .getByRole('button', { name: 'Ризик між ребалансами', exact: true })
    .click();
  const risk = page.locator('[data-node-id="result-4"]');
  await expect(risk).toContainText('Ліміти не задані');
  const limit = risk.getByRole('spinbutton', { name: 'Збиток від ціни купівлі', exact: true });
  await expect(limit).toHaveAttribute('placeholder', 'Вкажіть відсоток');
  await expect(limit).toBeEnabled();
  await limit.fill('10');
  await limit.press('Tab');
  await expect(risk.locator('[data-active-risk-limits]')).toHaveAttribute(
    'data-active-risk-limits',
    '1',
  );
  await expect
    .poll(
      () =>
        fixture.draft.graph.nodes.find((node: any) => node.id === 'result-4').parameters
          .stopLossFromCost,
    )
    .toBe(-0.1);
  await limit.fill('');
  await limit.press('Tab');
  await expect(risk).toContainText('Ліміти не задані');
  await page.getByRole('button', { name: 'Показати всю схему' }).click();
  const control = page.locator('[data-node-id="compare"] p-select');
  await control.getByRole('combobox').click();
  const menu = page.locator('.builder-node-select-panel:visible');
  await expect(menu).toBeVisible();
  await expect
    .poll(async () =>
      Math.abs((await menu.boundingBox())!.width - (await control.boundingBox())!.width),
    )
    .toBeLessThan(2);
  await expect(menu).toHaveCSS('font-size', '12px');
  await page.screenshot({ path: testInfo.outputPath('compact-node-menu-wide-palette.png') });
  await page.getByRole('option', { name: 'менше за', exact: true }).click();
  await expect
    .poll(
      () =>
        fixture.draft.graph.nodes.find((node: any) => node.id === 'compare').parameters.operator,
    )
    .toBe('lt');
});

test('review stays compact with a fixed close button, Escape and focus return', async ({
  page,
}, testInfo) => {
  await mockStrategyWorkspace(page, (draft) => {
    draft.words = ['MarketFilter', 'Exit', 'Entry', 'Selection', 'Risk'].map((block) => ({
      block,
      sentence: 'Стратегія перевіряє умови та виконує налаштовану дію для обраних фондів.',
    }));
  });
  await page.setViewportSize({ width: 2560, height: 1440 });
  await page.goto('/strategy-builder/workspace');
  const toggle = page.getByRole('button', { name: 'Без зауважень' });
  const panel = page.locator('#strategy-review');
  await toggle.click();
  await expect(panel).toBeVisible();
  expect((await panel.boundingBox())!.width).toBeLessThanOrEqual(1120);
  expect((await panel.boundingBox())!.height).toBeLessThan(380);
  await expect(panel.locator('.review-content')).not.toHaveClass(/has-problems/);
  await expect(panel.getByRole('heading', { name: 'Вихід', exact: true })).toHaveCSS(
    'font-size',
    '13px',
  );
  await page.screenshot({ path: testInfo.outputPath('compact-review-wide.png') });
  await panel.getByRole('button', { name: 'Закрити опис і перевірку' }).click();
  await expect(panel).toHaveCount(0);
  await expect(toggle).toBeFocused();
  await expect(toggle).toHaveAttribute('aria-expanded', 'false');
  await toggle.click();
  await page.keyboard.press('Escape');
  await expect(panel).toHaveCount(0);
  await expect(toggle).toBeFocused();
  await page.setViewportSize({ width: 390, height: 844 });
  await page.getByRole('button', { name: 'Більше дій', exact: true }).click();
  await page.getByRole('menuitem', { name: /Опис і перевірка/ }).click();
  await expect(panel).toBeVisible();
  await panel.locator('.review-content').evaluate((el) => (el.scrollTop = el.scrollHeight));
  await expect(panel.getByRole('button', { name: 'Закрити опис і перевірку' })).toBeInViewport();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: testInfo.outputPath('compact-review-mobile.png') });
  await panel.getByRole('button', { name: 'Закрити опис і перевірку' }).click();
  await expect(panel).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Схема', exact: true })).toHaveCount(0);
});
