import { expect, test } from '@playwright/test';
import { mockStrategyWorkspace } from './support/strategy-workspace';

const ids = {
  fund: '0199f000-0000-7000-8000-00000000000a',
  benchmark: '0199f000-0000-7000-8000-0000000000be',
  other: '0199f000-0000-7000-8000-0000000000bf',
  cash: '0199f000-0000-7000-8000-0000000000ca',
  vix: '0199f000-0000-7000-8000-0000000000aa',
  extra: '0199f000-0000-7000-8000-0000000000a1',
};

test('setup is permanent, each field has a socket, and bindings follow changed settings', async ({
  page,
}, testInfo) => {
  const fixture = await mockStrategyWorkspace(page);
  fixture.draft.setup = {
    funds: [ids.fund],
    benchmarkId: ids.benchmark,
    cashId: ids.cash,
    volatilityIndexId: ids.vix,
    extraSeries: [ids.extra],
  };
  await page.route('**/api/strategy-builder/instruments', (route) =>
    route.fulfill({
      json: Object.entries(ids).map(([name, id]) => ({
        id,
        label: name,
        name,
        group: 'Demo',
        assetClass: 'ETF',
        region: null,
        isVolatilityIndex: name === 'vix',
      })),
    }),
  );
  const errors: string[] = [];
  page.on('pageerror', (error) => errors.push(error.message));
  await page.goto('/strategy-builder/workspace');
  const setup = page.locator('[data-node-id="strategy-setup"]');
  await expect(setup).toHaveCount(1);
  for (const name of ['Фонди', 'Бенчмарк', 'Індекс волатильності', 'Додаткові ряди', 'extra'])
    await expect(setup.getByRole('button', { name: `Вихід ${name}`, exact: true })).toHaveCount(1);
  await expect(setup.getByText('Усі вибрані фонди', { exact: true })).toHaveCount(0);
  await expect(setup.locator('[data-socket-key$=":output:universe"]')).toHaveCount(0);
  await expect(
    page.locator('path[data-edge-from="strategy-setup"][data-edge-input="funds"]'),
  ).toHaveAttribute('data-edge-output', 'funds');
  await expect(
    page
      .locator('[data-node-id="strategy-cash"]')
      .getByRole('button', { name: 'Вихід Ряд кешу', exact: true }),
  ).toHaveCount(1);
  await expect(setup.getByRole('button', { name: 'Видалити вузол', exact: true })).toHaveCount(0);
  await setup.focus();
  await page.keyboard.press('Delete');
  await expect(setup).toHaveCount(1);
  await page.getByRole('button', { name: 'Показати всю схему' }).click();
  const row = page.locator('[data-node-id="number"]');
  await expect(row).toHaveAttribute('data-evaluation-scope', 'per-fund');
  await setup.getByRole('button', { name: 'Вихід Бенчмарк', exact: true }).click();
  await row.getByRole('button', { name: 'Вхід of', exact: true }).click();
  await expect
    .poll(() =>
      fixture.draft.graph.links
        .filter((link: any) => link.to === 'number' && link.input === 'of')
        .map((link: any) => link.output),
    )
    .toEqual(['benchmarkId']);
  await expect(row).toHaveAttribute('data-evaluation-scope', 'shared');
  await page.getByRole('button', { name: /Налаштування/ }).click();
  await setup.locator('p-select:has(#setup-node-benchmarkId)').getByRole('combobox').click();
  await page.getByRole('option', { name: 'other', exact: true }).click();
  await expect.poll(() => fixture.draft.setup.benchmarkId).toBe(ids.other);
  await expect
    .poll(
      () =>
        fixture.draft.graph.links.find((link: any) => link.to === 'number' && link.input === 'of')
          ?.output,
    )
    .toBe('benchmarkId');
  await page.reload();
  await expect(setup).toHaveCount(1);
  await expect(page.locator('path[data-edge-to="number"][data-edge-input="of"]')).toHaveAttribute(
    'data-edge-output',
    'benchmarkId',
  );
  await page.getByRole('button', { name: 'На весь екран', exact: true }).click();
  await page
    .getByRole('navigation', { name: 'Ролі стратегії' })
    .getByRole('button', { name: 'Фонди й ряди', exact: true })
    .click();
  await setup.locator('p-select:has(#setup-node-benchmarkId)').getByRole('combobox').click();
  await expect(page.locator('.workspace .p-select-overlay:visible').last()).toBeVisible();
  await page.getByRole('option', { name: 'benchmark', exact: true }).click();
  await page.screenshot({ path: testInfo.outputPath('setup-node-fullscreen.png') });
  await page.getByRole('button', { name: 'Вийти з повноекранного режиму' }).click();
  expect(errors).toEqual([]);
});

test('group selection preserves other groups, supports mixed state and only toggles filtered matches', async ({
  page,
}, testInfo) => {
  const fixture = await mockStrategyWorkspace(page);
  const funds = [
    { id: ids.fund, label: 'Alpha', group: 'Акції' },
    { id: ids.benchmark, label: 'Beta', group: 'Акції' },
    { id: ids.other, label: 'Gamma', group: 'Акції' },
    { id: ids.cash, label: 'Cash', group: 'Облігації' },
  ].map((item) => ({ ...item, name: `${item.label} ETF`, isVolatilityIndex: false }));
  fixture.draft.setup.funds = [ids.fund, ids.cash];
  await page.route('**/api/strategy-builder/instruments', (route) =>
    route.fulfill({ json: funds }),
  );
  await page.goto('/strategy-builder/workspace');
  await page.getByRole('button', { name: /Налаштування/ }).click();
  const setup = page.locator('[data-node-id="strategy-setup"]');
  await setup.locator('app-builder-fund-picker p-multiselect').click();
  const group = page.getByRole('checkbox', { name: 'Уся група «Акції»', exact: true });
  await expect(group).toHaveAttribute('aria-checked', 'mixed');
  await page.locator('.builder-fund-picker-panel:visible').evaluate(async (panel) => {
    const overlay = panel.closest('.p-overlay') ?? panel.parentElement ?? panel;
    await Promise.all(
      overlay
        .getAnimations({ subtree: true })
        .map((animation) => animation.finished.catch(() => {})),
    );
  });
  await page.screenshot({ path: testInfo.outputPath('fund-group-hierarchy.png') });
  await group.click();
  await expect
    .poll(() => fixture.draft.setup.funds.toSorted())
    .toEqual(funds.map((item) => item.id).sort());
  await expect(group).toHaveAttribute('aria-checked', 'true');
  const filter = page.locator('.p-multiselect-overlay:visible input.p-multiselect-filter');
  await filter.fill('Beta');
  await group.click();
  await expect
    .poll(() => fixture.draft.setup.funds.toSorted())
    .toEqual([ids.fund, ids.other, ids.cash].sort());
  await filter.fill('');
  await expect(group).toHaveAttribute('aria-checked', 'mixed');
  await group.focus();
  await page.keyboard.press('Space');
  await expect(group).toHaveAttribute('aria-checked', 'true');
  await page.keyboard.press('Space');
  await expect.poll(() => fixture.draft.setup.funds).toEqual([ids.cash]);
  await page.keyboard.press('Escape');
  await page.reload();
  await expect(page.locator('[data-node-id="number"]')).toHaveAttribute(
    'data-iteration-count',
    '1',
  );
});

test('one funds socket feeds both candidates and indicator series and restores old links', async ({
  page,
}) => {
  const fixture = await mockStrategyWorkspace(page);
  fixture.draft.graph.nodes.push(
    { id: 'strategy-setup', kind: 'Setup', block: 'Shared', parameters: {} },
    { id: 'strategy-cash', kind: 'Cash', block: 'Shared', parameters: {} },
    {
      id: 'fund-selection',
      kind: 'FundSelection',
      block: 'Shared',
      parameters: { top: 3, order: 'desc' },
    },
  );
  fixture.draft.graph.links.push({
    from: 'strategy-setup',
    output: 'universe',
    to: 'result-1',
    input: 'funds',
  });
  await page.goto('/strategy-builder/workspace');
  const setup = page.locator('[data-node-id="strategy-setup"]');
  await expect(setup.getByRole('button', { name: 'Вихід Фонди', exact: true })).toHaveCount(1);
  await expect(setup.getByText('Усі вибрані фонди', { exact: true })).toHaveCount(0);
  await expect(
    page.locator('path[data-edge-from="strategy-setup"][data-edge-to="result-1"]'),
  ).toHaveAttribute('data-edge-output', 'funds');
  await page.getByRole('button', { name: 'Показати всю схему' }).click();
  const output = setup.getByRole('button', { name: 'Вихід Фонди', exact: true });
  await output.focus();
  await output.press('Enter');
  await page
    .locator('[data-node-id="result-2"]')
    .getByRole('button', { name: 'Вхід funds', exact: true })
    .press('Enter');
  await expect
    .poll(() =>
      fixture.draft.graph.links.some(
        (link: any) =>
          link.from === 'strategy-setup' && link.to === 'result-2' && link.output === 'funds',
      ),
    )
    .toBe(true);
  await output.press('Enter');
  await page
    .locator('[data-node-id="number"]')
    .getByRole('button', { name: 'Вхід of', exact: true })
    .press('Enter');
  await expect
    .poll(() =>
      fixture.draft.graph.links.some(
        (link: any) =>
          link.from === 'strategy-setup' && link.to === 'number' && link.output === 'funds',
      ),
    )
    .toBe(true);
  await page.reload();
  await expect(
    page.locator('path[data-edge-from="strategy-setup"][data-edge-to="number"]'),
  ).toHaveAttribute('data-edge-output', 'funds');
});
