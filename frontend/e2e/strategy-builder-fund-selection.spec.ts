import { expect, test } from '@playwright/test';
import { chooseFromEditorMenu } from './support/editor-menu';

test('trend template saves an entire fund group and shows per-fund and shared evaluation after reload', async ({
  page,
}, testInfo) => {
  test.skip(!process.env['E2E_LIVE_TEMPLATES'], 'Uses a local API and a disposable test account.');
  await page.setViewportSize({ width: 1366, height: 768 });
  await page.goto('/auth/register');
  await page.locator('#email').fill(`fund-selection-${Date.now()}@example.com`);
  await page.locator('#password').fill('correct horse battery');
  await page.locator('#confirm').fill('correct horse battery');
  await page.getByRole('button', { name: /Зареєструватися/ }).click();
  await expect(page.getByRole('heading', { name: 'Огляд', exact: true })).toBeVisible();

  const directoryResponse = await page.request.get('/api/strategy-builder/instruments');
  expect(directoryResponse.ok()).toBe(true);
  const directory = await directoryResponse.json();
  const funds = directory
    .filter((item: { isVolatilityIndex: boolean }) => !item.isVolatilityIndex)
    .slice(0, 3)
    .map((item: { id: string }) => ({ ...item, group: 'Тестова група' }));
  expect(funds).toHaveLength(3);
  const expectedIds = funds.map((fund: { id: string }) => fund.id).sort();
  await page.route('**/api/strategy-builder/instruments', (route) =>
    route.fulfill({ json: funds }),
  );

  const created = await page.request.post('/api/strategy-builder/drafts', {
    data: { name: 'Перебір групи фондів', template: 'momentum-trend-filter' },
  });
  expect(created.ok()).toBe(true);
  const draft = await created.json();
  try {
    await page.goto(`/strategy-builder/${draft.id}`);
    await chooseFromEditorMenu(page, 'Стратегія', 'Фонди й ряди…');
    const setup = page.locator('[data-node-id="strategy-setup"]');
    await setup.locator('.picker-button').first().click();
    const dialog = page.getByRole('dialog', { name: 'Фонди стратегії', exact: true });
    const group = dialog.locator('.fund-group .group-title').getByRole('checkbox');
    await expect(group).not.toBeChecked();
    await group.check();
    const savedResponse = page.waitForResponse(
      (response) =>
        response.url().endsWith(`/drafts/${draft.id}`) && response.request().method() === 'PUT',
    );
    await dialog.getByRole('button', { name: 'Застосувати · 3', exact: true }).click();
    const saved = await savedResponse;
    expect(saved.ok(), await saved.text()).toBe(true);
    expect(saved.request().postDataJSON().setup.funds.toSorted()).toEqual(expectedIds);
    await expect(dialog).toBeHidden();
    await expect(setup.locator('.fund-token')).toHaveCount(3);
    await expect(page.locator('[data-node-id="e-mom"]')).toHaveAttribute(
      'data-iteration-count',
      '3',
    );
    await expect(page.locator('[data-node-id="e-all"]')).toHaveClass(/multi-fund/);
    await expect(page.locator('[data-node-id="m-price"]')).toHaveAttribute(
      'data-evaluation-scope',
      'shared',
    );
    await expect(page.locator('[data-node-id="result-selection"]')).toHaveAttribute(
      'data-evaluation-scope',
      'portfolio',
    );
    await page.getByRole('button', { name: 'Показати всю схему' }).click();
    await page.screenshot({ path: testInfo.outputPath('trend-fund-iteration.png') });
    await page
      .getByRole('navigation', { name: 'Ролі стратегії' })
      .getByRole('button', { name: 'Вхід', exact: true })
      .click();
    await page.screenshot({ path: testInfo.outputPath('trend-fund-loop-branch.png') });

    await page.reload();
    const persisted = await (
      await page.request.get(`/api/strategy-builder/drafts/${draft.id}`)
    ).json();
    expect(persisted.setup.funds.toSorted()).toEqual(expectedIds);
    expect(
      persisted.graph.nodes.filter((node: { kind: string }) => node.kind === 'Setup'),
    ).toHaveLength(1);
    await expect(page.locator('[data-node-id="e-mom"]')).toHaveAttribute(
      'data-iteration-count',
      '3',
    );
    await chooseFromEditorMenu(page, 'Стратегія', 'Фонди й ряди…');
    await setup.locator('.picker-button').first().click();
    await expect(group).toBeChecked();
    await page.screenshot({ path: testInfo.outputPath('selected-fund-group.png') });
    await dialog.getByRole('button', { name: 'Скасувати', exact: true }).click();
  } finally {
    await page.request.delete(`/api/strategy-builder/drafts/${draft.id}`);
  }
});
