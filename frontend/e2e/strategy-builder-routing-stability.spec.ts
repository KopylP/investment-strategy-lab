import { expect, test } from '@playwright/test';
import {
  retargetRoute,
  routeIsClear,
  routePoints,
} from '../src/app/features/strategy-builder/ui/graph/edge-routing';
import { mockStrategyWorkspace } from './support/strategy-workspace';

test('retargeting preserves channels, translates shared motion and checks obstacles', () => {
  const points = [
    { x: 100, y: 100 },
    { x: 300, y: 100 },
    { x: 300, y: 300 },
    { x: 600, y: 300 },
  ];
  const shifted = retargetRoute(points, { x: 112, y: 116 }, { x: 600, y: 300 });
  expect(shifted).toEqual([
    { x: 112, y: 116 },
    { x: 300, y: 116 },
    { x: 300, y: 300 },
    { x: 600, y: 300 },
  ]);
  expect(retargetRoute(points, { x: 120, y: 130 }, { x: 620, y: 330 })).toEqual(
    points.map((p) => ({ x: p.x + 20, y: p.y + 30 })),
  );
  const boxes = [
    { id: 'from', x: 0, y: 0, width: 100, height: 160 },
    { id: 'to', x: 600, y: 220, width: 100, height: 160 },
    { id: 'obstacle', x: 260, y: 180, width: 80, height: 80 },
  ];
  expect(routeIsClear(points, boxes, 'from', 'to')).toBe(false);
  expect(
    routeIsClear(routePoints(points[0], points.at(-1)!, boxes, 'from', 'to'), boxes, 'from', 'to'),
  ).toBe(true);
});

test('dragging preserves unrelated wires and smoothly follows the connected socket', async ({
  page,
}, testInfo) => {
  const workspace = await mockStrategyWorkspace(page);
  await page.setViewportSize({ width: 2560, height: 1440 });
  await page.goto('/strategy-builder/workspace');
  await expect.poll(() => Object.keys(workspace.draft.layout.positions).length).toBeGreaterThan(5);
  await page
    .getByRole('navigation', { name: 'Ролі стратегії' })
    .getByRole('button', { name: 'Ринковий фільтр', exact: true })
    .click();
  const source = page.locator('[data-node-id="condition-0"]');
  const connected = page.locator('path[data-edge-from="condition-0"][data-edge-to="result-0"]');
  const otherWires = page.locator(
    'path[data-edge-from]:not([data-edge-from="condition-0"]):not([data-edge-to="condition-0"])',
  );
  await expect(connected).toBeVisible();
  const paths = () =>
    otherWires.evaluateAll((els) =>
      els.map((el) => ({
        from: el.getAttribute('data-edge-from'),
        to: el.getAttribute('data-edge-to'),
        d: el.getAttribute('d'),
      })),
    );
  const before = await paths();
  const beforePosition = { ...workspace.draft.layout.positions['condition-0'] };
  const geometry = () =>
    connected.evaluate((el) => {
      const p = el as SVGPathElement;
      return Array.from({ length: 21 }, (_, i) => {
        const q = p.getPointAtLength((p.getTotalLength() * i) / 20);
        return { x: q.x, y: q.y };
      });
    });
  let previous = await geometry();
  const box = (await source.boundingBox())!;
  await page.mouse.move(box.x + 30, box.y + 8);
  await page.mouse.down();
  for (let step = 1; step <= 6; step++) {
    await page.mouse.move(box.x + 30 + step * 2, box.y + 8 + step * 3);
    await page.evaluate(() => new Promise(requestAnimationFrame));
    await expect.poll(paths).toEqual(before);
    const next = await geometry();
    expect(
      Math.max(...next.map((p, i) => Math.hypot(p.x - previous[i].x, p.y - previous[i].y))),
    ).toBeLessThan(35);
    previous = next;
  }
  await page.mouse.up();
  await expect
    .poll(() => workspace.draft.layout.positions['condition-0'])
    .not.toEqual(beforePosition);
  await expect.poll(paths).toEqual(before);
  await page.screenshot({ path: testInfo.outputPath('stable-routing-after-drag.png') });
});

test('moving a socket preserves the edited route through save and reload', async ({ page }) => {
  const workspace = await mockStrategyWorkspace(page);
  await page.setViewportSize({ width: 2560, height: 1440 });
  await page.goto('/strategy-builder/workspace');
  const wire = page.locator('path[data-edge-from="condition-0"][data-edge-to="result-0"]');
  await expect.poll(() => Object.keys(workspace.draft.layout.positions).length).toBeGreaterThan(5);
  const points = await wire.evaluate((el) => {
    const path = el as SVGPathElement,
      start = path.getPointAtLength(0),
      end = path.getPointAtLength(path.getTotalLength());
    const x = (start.x + end.x) / 2;
    return [
      { x, y: start.y },
      { x, y: end.y },
    ];
  });
  const key = '11:condition-0|6:output|8:result-0|9:condition';
  workspace.draft.layout.edgeRoutes = { [key]: points };
  await page.reload();
  await page
    .getByRole('navigation', { name: 'Ролі стратегії' })
    .getByRole('button', { name: 'Ринковий фільтр', exact: true })
    .click();
  await expect(wire).toHaveAttribute('d', /^M /);
  const source = page.locator('[data-node-id="condition-0"]');
  const beforePosition = { ...workspace.draft.layout.positions['condition-0'] };
  const box = (await source.boundingBox())!;
  await page.mouse.move(box.x + 30, box.y + 8);
  await page.mouse.down();
  await page.mouse.move(box.x + 38, box.y + 28, { steps: 6 });
  await page.mouse.up();
  await expect
    .poll(() => workspace.draft.layout.positions['condition-0'])
    .not.toEqual(beforePosition);
  await expect.poll(() => workspace.draft.layout.edgeRoutes[key]).not.toEqual(points);
  const saved = await wire.getAttribute('d');
  await page.reload();
  await expect(wire).toHaveAttribute('d', saved!);
});
