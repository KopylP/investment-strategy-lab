import { expect, test } from '@playwright/test';
import { mockStrategyWorkspace } from './support/strategy-workspace';

test('one shared branch feeds entry and exit while market context and cycles are protected', async ({
  page,
}, testInfo) => {
  const workspace = await mockStrategyWorkspace(page);
  const errors: string[] = [];
  page.on('pageerror', (error) => errors.push(error.message));
  await page.goto('/strategy-builder/workspace');
  await expect(page.locator('app-node-palette').getByRole('combobox')).toHaveCount(0);
  await page.getByRole('button', { name: 'Показати всю схему' }).click();
  const output = (id: string) =>
    page.locator(`[data-node-id="${id}"]`).getByRole('button', { name: 'Почати звʼязок' });
  const input = (id: string, key: string) =>
    page
      .locator(`[data-node-id="${id}"]`)
      .getByRole('button', { name: `Вхід ${key}`, exact: true });
  // Use the keyboard to connect nodes even when a large graph is zoomed out.
  const wire = async (from: string, to: string, key: string) => {
    await output(from).focus();
    await output(from).press('Enter');
    await input(to, key).focus();
    await input(to, key).press('Enter');
  };
  await wire('number', 'compare', 'left');
  await wire('compare', 'result-2', 'condition');
  await expect
    .poll(() =>
      workspace.draft.graph.links.some(
        (link: any) => link.from === 'compare' && link.to === 'result-2',
      ),
    )
    .toBe(true);
  await wire('compare', 'result-1', 'condition');
  await expect
    .poll(() => workspace.draft.graph.links.filter((link: any) => link.from === 'compare').length)
    .toBe(2);
  await expect(page.locator('[data-node-id="compare"] .role')).toHaveText('Спільний');
  await wire('compare', 'result-0', 'condition');
  await expect(page.getByRole('alert').filter({ hasText: 'Ринковий фільтр' })).toBeVisible();
  await expect
    .poll(() =>
      workspace.draft.graph.links
        .filter((link: any) => link.to === 'result-0')
        .map((link: any) => link.from),
    )
    .toEqual(['condition-0']);
  await page.getByRole('button', { name: 'Скасувати', exact: true }).click();
  // Connecting into a condition already used by the market is also rejected.
  await wire('compare', 'condition-0', 'condition');
  await expect(page.getByRole('alert').filter({ hasText: 'Ринковий фільтр' })).toBeVisible();
  await page.getByRole('button', { name: 'Скасувати', exact: true }).click();
  await page.reload();
  await expect(page.locator('[data-node-id="compare"] .role')).toHaveText('Спільний');
  await expect(page.locator('path[data-edge-from="compare"]')).toHaveCount(2);
  await wire('condition-1', 'condition-2', 'condition');
  await wire('condition-2', 'condition-1', 'condition');
  await expect(page.getByRole('alert').filter({ hasText: 'цикл' })).toBeVisible();
  await page.getByRole('button', { name: 'Скасувати', exact: true }).click();
  await page.getByRole('button', { name: 'Показати всю схему' }).click();
  await page.screenshot({ path: testInfo.outputPath('shared-graph.png') });
  expect(errors).toEqual([]);
});
