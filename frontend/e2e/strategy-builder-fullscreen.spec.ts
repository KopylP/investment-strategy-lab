import { expect, test } from '@playwright/test';
import { mockStrategyWorkspace } from './support/strategy-workspace';

test('fullscreen expands the workspace independently of fit and keeps dropdowns usable', async ({
  page,
}, testInfo) => {
  const errors: string[] = [];
  page.on('pageerror', (error) => errors.push(error.message));
  await mockStrategyWorkspace(page);
  await page.goto('/strategy-builder/workspace');
  const workspace = page.locator('app-graph-editor .workspace');
  await expect(workspace).toBeVisible();
  const initial = (await workspace.boundingBox())!;
  const zoom = await workspace.locator('output').innerText();
  const enter = page.getByRole('button', { name: 'На весь екран', exact: true });
  await expect(enter.locator('i')).toHaveClass(/pi-expand/);
  await expect(page.getByRole('button', { name: 'Показати всю схему' }).locator('i')).toHaveClass(
    /pi-arrows-alt/,
  );
  await enter.click();
  await expect.poll(() => workspace.evaluate((el) => document.fullscreenElement === el)).toBe(true);
  await expect(workspace.locator('output')).toHaveText(zoom);
  await expect(page.getByRole('button', { name: 'Вийти з повноекранного режиму' })).toHaveAttribute(
    'aria-pressed',
    'true',
  );
  const screen = await workspace.evaluate((el) => {
    const r = el.getBoundingClientRect();
    return {
      x: r.x,
      y: r.y,
      width: r.width,
      height: r.height,
      viewportWidth: innerWidth,
      viewportHeight: innerHeight,
    };
  });
  expect(screen).toEqual({
    x: 0,
    y: 0,
    width: screen.viewportWidth,
    height: screen.viewportHeight,
    viewportWidth: screen.viewportWidth,
    viewportHeight: screen.viewportHeight,
  });
  expect(screen.height).toBeGreaterThan(initial.height);
  await page.getByRole('button', { name: 'Показати всю схему' }).click();
  await expect.poll(() => workspace.evaluate((el) => document.fullscreenElement === el)).toBe(true);
  await workspace
    .getByRole('navigation', { name: 'Ролі стратегії' })
    .getByRole('button', { name: 'Ринковий фільтр', exact: true })
    .click();
  await workspace.locator('[data-node-id="signal"]').getByRole('combobox').click();
  await expect(workspace.locator('.p-select-overlay:visible').last()).toBeVisible();
  await page.screenshot({ path: testInfo.outputPath('fullscreen-dropdown.png') });
  await workspace.locator('[data-node-id="signal"] header').click();
  await page.getByRole('button', { name: 'Вийти з повноекранного режиму' }).click();
  await expect.poll(() => page.evaluate(() => document.fullscreenElement)).toBe(null);
  await expect(enter).toHaveAttribute('aria-pressed', 'false');
  await enter.click();
  await expect.poll(() => workspace.evaluate((el) => document.fullscreenElement === el)).toBe(true);
  // External exits (including browser Escape) update the toggle through fullscreenchange.
  await page.evaluate(() => document.exitFullscreen());
  await expect(enter).toHaveAttribute('aria-pressed', 'false');
  expect(errors).toEqual([]);
});
