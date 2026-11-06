import { expect, Locator, Page, test } from '@playwright/test';
import { mockStrategyWorkspace } from './support/strategy-workspace';

async function clickWire(page: Page, wire: Locator) {
  await expect(async () => {
    const p = await wire.evaluate((element) => {
      const path = element as SVGPathElement;
      const hit = path.parentElement!.querySelector('.hit');
      for (const fraction of [0.5, 0.25, 0.75, 0.1, 0.9]) {
        const point = path
          .getPointAtLength(path.getTotalLength() * fraction)
          .matrixTransform(path.getScreenCTM()!);
        if (document.elementFromPoint(point.x, point.y) === hit) return { x: point.x, y: point.y };
      }
      return null;
    });
    expect(p).not.toBeNull();
    await page.mouse.click(p!.x, p!.y);
    await expect(wire).toHaveAttribute('data-edge-selected', 'true', { timeout: 1000 });
  }).toPass({ timeout: 5000 });
}

test('select, reshape, persist, reset and delete wires with undo and redo', async ({
  page,
}, testInfo) => {
  const workspace = await mockStrategyWorkspace(page);
  const errors: string[] = [];
  page.on('pageerror', (error) => errors.push(error.message));
  await page.goto('/strategy-builder/workspace');
  const wire = page.locator('path[data-edge-from="condition-0"][data-edge-to="result-0"]');
  await expect(wire).toHaveCount(1);
  await expect.poll(() => Object.keys(workspace.draft.layout.positions).length).toBeGreaterThan(5);
  await page.getByRole('button', { name: 'Показати всю схему' }).click();
  await clickWire(page, wire);
  await expect(wire).toHaveAttribute('data-edge-selected', 'true');
  await expect(page.locator('path[data-edge-selected="true"]')).toHaveCount(1);
  const original = await wire.getAttribute('d');
  const handle = page.locator('circle[data-route-axis="y"]').first();
  await expect(handle).toBeVisible();
  const axis = await handle.getAttribute('data-route-axis');
  const box = (await handle.boundingBox())!;
  await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
  await page.mouse.down();
  await page.mouse.move(
    box.x + box.width / 2 + (axis === 'x' ? 35 : 0),
    box.y + box.height / 2 + (axis === 'y' ? -35 : 0),
    { steps: 8 },
  );
  await page.mouse.up();
  await expect.poll(() => Object.keys(workspace.draft.layout.edgeRoutes ?? {}).length).toBe(1);
  await expect(wire).not.toHaveAttribute('d', original!);
  const saved = await wire.getAttribute('d');
  const route = structuredClone(workspace.draft.layout.edgeRoutes);
  const points = Object.values(route)[0] as { x: number; y: number }[];
  for (let i = 1; i < points.length; i++)
    expect(points[i].x === points[i - 1].x || points[i].y === points[i - 1].y).toBe(true);
  await page.screenshot({ path: testInfo.outputPath('selected-reshaped-wire.png') });
  await page.reload();
  await expect(wire).toHaveAttribute('d', saved!);
  await clickWire(page, wire);
  await expect(wire).toHaveAttribute('data-edge-selected', 'true');
  // Reshape a vertical segment horizontally, as in draw.io.
  const vertical = page.locator('circle[data-route-axis="x"]').first();
  if (await vertical.count()) {
    const before = structuredClone(workspace.draft.layout.edgeRoutes);
    await vertical.focus();
    await vertical.press('ArrowRight');
    await expect.poll(() => workspace.draft.layout.edgeRoutes).not.toEqual(before);
    await page.getByRole('button', { name: 'Скасувати дію', exact: true }).click();
    await expect.poll(() => workspace.draft.layout.edgeRoutes).toEqual(route);
    await page.getByRole('button', { name: 'Повторити дію', exact: true }).click();
    await expect.poll(() => workspace.draft.layout.edgeRoutes).not.toEqual(route);
  }
  await clickWire(page, wire);
  await page.getByRole('button', { name: 'Видалити зв’язок', exact: true }).click();
  await expect(wire).toHaveCount(0);
  await expect.poll(() => Object.keys(workspace.draft.layout.edgeRoutes ?? {}).length).toBe(0);
  await page.getByRole('button', { name: 'Скасувати дію', exact: true }).click();
  await expect(wire).toHaveCount(1);
  await expect.poll(() => Object.keys(workspace.draft.layout.edgeRoutes ?? {}).length).toBe(1);
  await clickWire(page, wire);
  await page.getByRole('button', { name: 'Автоматичний маршрут', exact: true }).click();
  await expect.poll(() => Object.keys(workspace.draft.layout.edgeRoutes ?? {}).length).toBe(0);
  await page.keyboard.press('Delete');
  await expect(wire).toHaveCount(0);
  expect(errors).toEqual([]);
});

test('a saved route returning along the same vertical lane has no dangling tail', async ({
  page,
}) => {
  const workspace = await mockStrategyWorkspace(page);
  await page.goto('/strategy-builder/workspace');
  const wire = page.locator('path[data-edge-from="condition-0"][data-edge-to="result-0"]');
  await expect.poll(() => Object.keys(workspace.draft.layout.positions).length).toBeGreaterThan(5);
  const ends = await wire.evaluate((element) => {
    const path = element as SVGPathElement;
    const start = path.getPointAtLength(0),
      end = path.getPointAtLength(path.getTotalLength());
    return { start: { x: start.x, y: start.y }, end: { x: end.x, y: end.y } };
  });
  const x = (ends.start.x + ends.end.x) / 2;
  workspace.draft.layout.edgeRoutes = {
    '11:condition-0|6:output|8:result-0|9:condition': [
      { x, y: ends.start.y },
      { x: x + 24, y: ends.start.y },
      { x, y: ends.end.y + 300 },
      { x, y: ends.end.y },
    ],
  };
  await page.reload();
  await expect(wire).toHaveCount(1);
  await expect
    .poll(() =>
      wire.evaluate((element) => {
        const path = element as SVGPathElement,
          box = path.getBBox();
        const start = path.getPointAtLength(0),
          end = path.getPointAtLength(path.getTotalLength());
        return box.y + box.height - Math.max(start.y, end.y);
      }),
    )
    .toBeLessThan(1);
});
