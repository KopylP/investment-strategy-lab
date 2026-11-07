import { expect, test } from '@playwright/test';
import { mockStrategyWorkspace } from './support/strategy-workspace';

test('Laptop strategy workspace leaves room for the canvas', async ({ page }, testInfo) => {
  const errors: string[] = [];
  page.on('pageerror', (e) => errors.push(e.message));
  await mockStrategyWorkspace(page);
  await page.goto('/strategy-builder/workspace');
  await page.getByRole('button', { name: 'Схема', exact: true }).click();

  await expect(page.locator('[data-block-canvas]')).toHaveCount(1);
  for (const [width, height] of [
    [1280, 720],
    [1440, 800],
    [1512, 870],
  ]) {
    await page.setViewportSize({ width, height });
    await page.waitForTimeout(500);
    const metrics = await page.locator('[data-block-canvas]').evaluate((el) => {
      const r = el.getBoundingClientRect();
      return {
        viewport: [innerWidth, innerHeight],
        canvas: { x: r.x, y: r.y, width: r.width, height: r.height },
        visibleHeight: Math.max(0, Math.min(r.bottom, innerHeight) - Math.max(0, r.top)),
        visibleArea:
          (Math.max(0, Math.min(r.right, innerWidth) - Math.max(0, r.left)) *
            Math.max(0, Math.min(r.bottom, innerHeight) - Math.max(0, r.top))) /
          (innerWidth * innerHeight),
      };
    });
    console.log(JSON.stringify(metrics));
    await page.screenshot({ path: testInfo.outputPath(`laptop-${width}.png`) });
    if (!process.env['LAYOUT_BASELINE']) {
      expect(metrics.canvas.y).toBeLessThan(220);
      expect(metrics.visibleHeight).toBeGreaterThan(height * 0.6);
      expect(metrics.visibleArea).toBeGreaterThan(0.48);
      expect(metrics.canvas.y + metrics.canvas.height).toBeLessThanOrEqual(height);
    }
  }
  const sizes = await page
    .locator('app-strategy-rete-node article')
    .evaluateAll((nodes) =>
      nodes.map((el) => ({
        kind: el.getAttribute('data-node-category'),
        width: (el as HTMLElement).offsetWidth,
        height: (el as HTMLElement).offsetHeight,
      })),
    );
  expect(sizes.every((node) => node.width <= 256)).toBe(true);
  expect(sizes.filter((node) => node.kind === 'Логіка').every((node) => node.height <= 110)).toBe(
    true,
  );
  await page.getByRole('button', { name: 'Меню', exact: true }).click();
  await expect(page.locator('.layout-wrapper')).toHaveClass(/layout-overlay-active/);
  await page.getByRole('button', { name: 'Меню', exact: true }).click();
  await page.getByRole('button', { name: 'Кроки', exact: true }).click();
  await expect(page.locator('.layout-wrapper')).toHaveClass(/layout-static/);
  expect(errors).toEqual([]);
});
