import { expect, test } from '@playwright/test';
import { mockStrategyWorkspace } from './support/strategy-workspace';

test('Rete workspace supports inline fields, palette drop, wires and navigation', async ({
  page,
}, testInfo) => {
  const errors: string[] = [];
  page.on('pageerror', (e) => errors.push(e.message));
  const workspace = await mockStrategyWorkspace(page);
  await page.goto('/strategy-builder/workspace');
  await page.getByText('Схема', { exact: true }).click();

  await expect(page.locator('[data-block-canvas]')).toHaveCount(1);
  const assertEdgesMeetSockets = async () => {
    await expect
      .poll(() =>
        page.evaluate(() => {
          const issues: unknown[] = [];
          for (const path of document.querySelectorAll<SVGPathElement>('path[data-edge-from]')) {
            const matrix = path.getScreenCTM();
            if (!matrix || !path.getAttribute('d')) {
              issues.push({ missing: path.dataset });
              continue;
            }
            const points = [path.getPointAtLength(0), path.getPointAtLength(path.getTotalLength())];
            const keys = [
              path.dataset['edgeOutput'] && path.dataset['edgeOutput'] !== 'output'
                ? `${path.dataset['edgeFrom']}:output:${path.dataset['edgeOutput']}`
                : `${path.dataset['edgeFrom']}:output`,
              `${path.dataset['edgeTo']}:input:${path.dataset['edgeInput']}`,
            ];
            keys.forEach((key, i) => {
              const socket = [...document.querySelectorAll<HTMLElement>('[data-socket-key]')].find(
                (el) => el.dataset['socketKey'] === key,
              );
              if (!socket) {
                issues.push({ key, missing: true });
                return;
              }
              const p = new DOMPoint(points[i].x, points[i].y).matrixTransform(matrix);
              const r = socket.getBoundingClientRect();
              const dx = p.x - r.left - r.width / 2,
                dy = p.y - r.top - r.height / 2;
              if (Math.hypot(dx, dy) >= 1)
                issues.push({ key, dx, dy, path: path.getAttribute('d') });
            });
          }
          return issues;
        }),
      )
      .toEqual([]);
  };
  const assertCenteredZoom = async () => {
    const viewport = () =>
      page.locator('[data-block-canvas]').evaluate((canvas) => {
        const holder = canvas.firstElementChild as HTMLElement;
        const transform = new DOMMatrix(getComputedStyle(holder).transform);
        return {
          scale: transform.a,
          worldX: (canvas.clientWidth / 2 - transform.e) / transform.a,
          worldY: (canvas.clientHeight / 2 - transform.f) / transform.a,
        };
      });
    const before = await viewport();
    await page.getByRole('button', { name: 'Збільшити', exact: true }).click();
    await expect.poll(async () => (await viewport()).scale).toBeGreaterThan(before.scale);
    const enlarged = await viewport();
    expect(Math.abs(enlarged.worldX - before.worldX) * enlarged.scale).toBeLessThan(0.5);
    expect(Math.abs(enlarged.worldY - before.worldY) * enlarged.scale).toBeLessThan(0.5);
    await page.getByRole('button', { name: 'Зменшити', exact: true }).click();
    await expect.poll(async () => (await viewport()).scale).toBeCloseTo(before.scale, 5);
    const restored = await viewport();
    expect(Math.abs(restored.worldX - before.worldX) * restored.scale).toBeLessThan(0.5);
    expect(Math.abs(restored.worldY - before.worldY) * restored.scale).toBeLessThan(0.5);
  };
  await assertEdgesMeetSockets();
  const canvas = page.locator('[data-block-canvas]');
  await expect(page.locator('app-graph-editor article')).toHaveCount(17);
  const outputs = page.locator('[data-node-id="result-3"] .outputs .port');
  await expect(outputs).toHaveCount(2);
  await expect(outputs.first()).toHaveAttribute('data-socket-key', 'result-3:output');
  const alignedOutputs = () =>
    outputs.evaluateAll((ports) => {
      const [top, bottom] = ports.map((port) => port.getBoundingClientRect());
      return Math.abs(top.right - bottom.right) < 0.5 && bottom.top >= top.bottom - 0.5;
    });
  await expect.poll(alignedOutputs).toBe(true);
  await assertCenteredZoom();
  await page.getByRole('button', { name: 'Показати всю схему' }).click();
  await page.getByRole('searchbox', { name: 'Пошук елементів' }).fill('Моментум');
  await expect(page.locator('app-node-palette button.item')).toHaveCount(1);
  await page.locator('app-node-palette button.item').click();
  await expect(page.locator('app-graph-editor article')).toHaveCount(18);
  await assertEdgesMeetSockets();
  const added = page.locator('app-graph-editor article').last();
  await expect(added.locator('app-node-inspector')).toBeVisible();
  const period = added.getByRole('spinbutton', { name: 'Період', exact: true });
  await period.fill('45');
  await period.press('Tab');
  await expect
    .poll(() => workspace.draft.graph.nodes.some((n: any) => n.parameters.period === 45))
    .toBe(true);
  const addedId = await added.getAttribute('data-node-id');
  await expect.poll(() => workspace.draft.layout.positions[addedId!]).toBeTruthy();
  const initial = { ...workspace.draft.layout.positions[addedId!] };
  await period.click();
  await period.press('ArrowUp');
  await period.press('Tab');
  expect(workspace.draft.layout.positions[addedId!]).toEqual(initial);
  await page.getByRole('searchbox').fill('');
  const box = (await canvas.boundingBox())!;
  const transfer = await page.evaluateHandle(() => new DataTransfer());
  await page
    .locator('app-node-palette button.item')
    .first()
    .dispatchEvent('dragstart', { dataTransfer: transfer });
  await canvas.dispatchEvent('drop', {
    dataTransfer: transfer,
    clientX: box.x + 250,
    clientY: box.y + 180,
  });
  await expect(page.locator('app-graph-editor article')).toHaveCount(19);
  await expect.poll(() => workspace.draft.graph.nodes.length).toBe(19);
  await page.getByRole('button', { name: 'Скасувати дію', exact: true }).click();
  await expect(page.locator('app-graph-editor article')).toHaveCount(18);
  await page.getByRole('button', { name: 'Повторити дію', exact: true }).click();
  await expect(page.locator('app-graph-editor article')).toHaveCount(19);
  await page.getByRole('button', { name: 'Показати всю схему' }).click();
  await page.getByRole('button', { name: 'Упорядкувати', exact: true }).click();
  await page
    .getByRole('navigation', { name: 'Ролі стратегії' })
    .getByRole('button', { name: 'Ринковий фільтр', exact: true })
    .click();
  // Include disconnected nodes before creating their first wire.
  await page.getByRole('button', { name: 'Показати всю схему' }).click();
  // Native Rete wires support both click-click and pointer drag.
  await assertEdgesMeetSockets();
  const source = page.locator('[data-node-id="number"]');
  const target = page.locator('[data-node-id="compare"]');
  await source.getByRole('button', { name: 'Почати звʼязок' }).click();
  await target.getByRole('button', { name: 'Вхід left', exact: true }).click();
  await expect
    .poll(() =>
      workspace.draft.graph.links.some((l: any) => l.from === 'number' && l.to === 'compare'),
    )
    .toBe(true);
  await expect(page.locator('path[data-edge-from="number"]')).toHaveAttribute('stroke', '#2563eb');
  const signal = page.locator('[data-node-id="signal"]');
  const result = page.locator('[data-node-id="result-0"]');
  await signal.getByRole('button', { name: 'Почати звʼязок' }).click();
  await result.getByRole('button', { name: 'Вхід condition', exact: true }).click();
  await expect
    .poll(() =>
      workspace.draft.graph.links.filter((l: any) => l.to === 'result-0').map((l: any) => l.from),
    )
    .toEqual(['signal']);
  await page.getByRole('button', { name: 'Скасувати дію', exact: true }).click();
  await expect
    .poll(() =>
      workspace.draft.graph.links.filter((l: any) => l.to === 'result-0').map((l: any) => l.from),
    )
    .toEqual(['condition-0']);
  await source.getByRole('button', { name: 'Почати звʼязок' }).click();
  await result.getByRole('button', { name: 'Вхід condition', exact: true }).click();
  await expect(page.getByRole('alert').filter({ hasText: 'тип даних' })).toBeVisible();
  await page.getByRole('button', { name: 'Скасувати', exact: true }).click();
  await page.getByRole('button', { name: 'Показати всю схему' }).click();
  const nodeBox = (await source.boundingBox())!;
  await page.mouse.move(nodeBox.x + 50, nodeBox.y + 8);
  await page.mouse.down();
  await page.mouse.move(nodeBox.x + 90, nodeBox.y + 28, { steps: 5 });
  await page.mouse.up();
  await page.getByRole('button', { name: 'Показати всю схему' }).click();
  await assertEdgesMeetSockets();
  await page
    .getByRole('navigation', { name: 'Ролі стратегії' })
    .getByRole('button', { name: 'Вхід', exact: true })
    .click();
  await assertEdgesMeetSockets();
  const panBox = (await canvas.boundingBox())!;
  await page.mouse.move(panBox.x + panBox.width - 16, panBox.y + panBox.height - 16);
  await page.mouse.down();
  await page.mouse.move(panBox.x + panBox.width - 96, panBox.y + panBox.height - 56, { steps: 5 });
  await page.mouse.up();
  await assertCenteredZoom();
  for (const width of [1440, 2560, 3440, 1024, 390]) {
    await page.setViewportSize({ width, height: 900 });
    if (width < 992)
      await expect
        .poll(async () => (await page.locator('.layout-main-container').boundingBox())!.x)
        .toBeLessThan(30);
    await page
      .getByRole('navigation', { name: 'Ролі стратегії' })
      .getByRole('button', { name: 'Вхід', exact: true })
      .click();
    expect(
      await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
    ).toBeTruthy();
    if (width >= 1960) {
      const workspace = (await page.locator('.workspace').boundingBox())!;
      expect(workspace.width).toBeGreaterThan(width - 32);
      expect(workspace.x).toBeLessThan(16);
    }
    await assertCenteredZoom();
    await assertEdgesMeetSockets();
    await page.screenshot({ path: testInfo.outputPath(`rete-${width}.png`), fullPage: true });
  }
  expect(errors).toEqual([]);
});
