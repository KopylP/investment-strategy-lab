import { expect, test } from '@playwright/test';
import { openPublishConfirmation } from './support/editor-menu';

test('one canvas supports selection, zoom, drag and responsive steps', async ({ page }, testInfo) => {
  const errors: string[] = [];
  page.on('pageerror', (e) => errors.push(e.message));
  const blocks = ['MarketFilter', 'Exit', 'Entry', 'Selection', 'Risk'];
  const kinds = ['MarketResult', 'ExitResult', 'EntryResult', 'SelectionResult', 'RiskResult'];
  const nodes = blocks.flatMap((block, i) => [
    { id: `condition-${i}`, kind: 'All', block, parameters: {} },
    { id: `result-${i}`, kind: kinds[i], block, parameters: {} },
  ]);
  nodes.push(
    { id: 'number', kind: 'Indicator', block: 'MarketFilter', parameters: {} },
    { id: 'compare', kind: 'Compare', block: 'MarketFilter', parameters: {} },
    { id: 'signal', kind: 'Signal', block: 'MarketFilter', parameters: {} },
  );
  let draft: any = {
    id: 'workspace',
    name: 'Перевірка UX',
    description: '',
    revision: 1,
    updatedAt: new Date().toISOString(),
    originStrategyId: null,
    originVersion: null,
    setup: { funds: [], benchmarkId: null, cashId: null, volatilityIndexId: null, extraSeries: [] },
    graph: {
      nodes,
      links: blocks.map((_, i) => ({
        from: `condition-${i}`,
        to: `result-${i}`,
        input: 'condition',
      })),
    },
    layout: {
      positions: Object.fromEntries(
        nodes.map((n, i) => [
          n.id,
          { x: i < 10 ? (i % 2 ? 500 : 32) : 32 + (i - 10) * 260, y: i < 10 ? 32 : 210 },
        ]),
      ),
      collapsed: [],
    },
    reports: [],
    problems: [],
    words: [],
  };
  const vocabulary = {
    indicators: [],
    conditions: [{ code: 'all', label: 'Усі умови', fields: [] }],
    blockFields: {},
    reports: [],
    templates: [],
    nodeKinds: ['All', ...kinds].map((kind) => ({
      kind,
      output: kind === 'All' ? 'Boolean' : null,
      inputs: [{ name: 'condition', type: 'Boolean', many: kind === 'All', required: true }],
    })),
  };
  vocabulary.nodeKinds.push(
    { kind: 'Indicator', output: 'Number', inputs: [] },
    {
      kind: 'Compare',
      output: 'Boolean',
      inputs: [{ name: 'left', type: 'Number', many: false, required: true }],
    },
    { kind: 'Signal', output: 'Boolean', inputs: [] },
  );
  let failSave = false;
  let conflict = false;
  let publishRequests = 0;
  await page.route('**/api/**', async (route) => {
    const path = new URL(route.request().url()).pathname;
    let body: any = [];
    if (path === '/api/identity/me') body = { id: 'test', email: 'test@example.com' };
    if (path === '/api/identity/profile')
      body = { displayCurrency: 'USD', locale: 'uk-UA', timeZone: 'Europe/Warsaw' };
    if (path.endsWith('/vocabulary')) body = vocabulary;
    if (path.endsWith('/drafts/workspace')) {
      if (route.request().method() === 'PUT') {
        if (conflict) return route.fulfill({ status: 409, json: { code: 'builder.draft.stale' } });
        if (failSave) return route.fulfill({ status: 500, json: { detail: 'Save failed' } });
        draft = { ...draft, ...route.request().postDataJSON(), revision: draft.revision + 1 };
      }
      body = draft;
    }
    if (path.endsWith('/layout')) draft.layout = route.request().postDataJSON();
    if (path.endsWith('/publish')) publishRequests++;
    if (path.endsWith('/check'))
      body = {
        isClean: false,
        problems: [
          {
            message: 'Оберіть фонди',
            severity: 'Error',
            nodeId: null,
            block: null,
            reportCode: null,
          },
        ],
      };
    await route.fulfill({ json: body });
  });
  await page.goto('/strategy-builder/workspace');
  await page.getByRole('button', { name: 'Схема', exact: true }).click();
  await expect(page.locator('[data-block-canvas]')).toHaveCount(1);
  await expect(page.locator('app-graph-editor article')).toHaveCount(13);
  await page.getByRole('button', { name: 'Збільшити', exact: true }).click();
  await expect(page.locator('app-graph-editor')).toContainText('90%');
  const assertEdgesMeetSockets = async () => {
    await expect
      .poll(() =>
        page.evaluate(() => {
          const paths = [...document.querySelectorAll<SVGPathElement>('path[data-edge-from]')];
          return paths.every((path) => {
            const matrix = path.getScreenCTM();
            if (!matrix || !path.getAttribute('d')) return false;
            const start = path.getPointAtLength(0);
            const end = path.getPointAtLength(path.getTotalLength());
            const endpoints = [
              new DOMPoint(start.x, start.y).matrixTransform(matrix),
              new DOMPoint(end.x, end.y).matrixTransform(matrix),
            ];
            const keys = [
              `${path.dataset['edgeFrom']}:output`,
              `${path.dataset['edgeTo']}:input:${path.dataset['edgeInput']}`,
            ];
            return keys.every((key, index) => {
              const socket = [...document.querySelectorAll<HTMLElement>('[data-socket-key]')].find(
                (el) => el.dataset['socketKey'] === key,
              )!;
              const rect = socket.getBoundingClientRect();
              return (
                Math.hypot(
                  endpoints[index].x - rect.left - rect.width / 2,
                  endpoints[index].y - rect.top - rect.height / 2,
                ) < 1
              );
            });
          });
        }),
      )
      .toBe(true);
  };
  await assertEdgesMeetSockets();
  const node = page.locator('app-graph-editor article').first();
  await node.click();
  await expect(page.locator('app-node-inspector')).toBeVisible();
  await node.getByRole('button', { name: 'Почати звʼязок' }).click();
  await node.getByRole('button', { name: 'Вхід condition', exact: true }).click();
  await expect(page.getByRole('alert').filter({ hasText: 'створює цикл' })).toBeVisible();
  await page.getByRole('button', { name: 'Скасувати', exact: true }).click();
  const source = page.locator('[data-node-id="number"]');
  await source.getByRole('button', { name: 'Почати звʼязок' }).click();
  await node.getByRole('button', { name: 'Вхід condition', exact: true }).click();
  await expect(page.getByRole('alert').filter({ hasText: 'очікує' })).toBeVisible();
  await page
    .locator('[data-node-id="result-1"]')
    .getByRole('button', { name: 'Вхід condition', exact: true })
    .click();
  await expect(page.getByRole('alert').filter({ hasText: 'Різні ролі' })).toBeVisible();
  const compatible = page
    .locator('[data-node-id="compare"]')
    .getByRole('button', { name: 'Вхід left', exact: true });
  await expect(compatible).toHaveClass(/ring-green-300/);
  await compatible.click();
  await expect
    .poll(() =>
      draft.graph.links.some((link: any) => link.from === 'number' && link.to === 'compare'),
    )
    .toBe(true);
  await assertEdgesMeetSockets();
  const blue = page.locator('path[data-edge-from="number"]');
  await expect(blue).toHaveAttribute('stroke', '#2563eb');
  await expect(page.locator('path[data-edge-from="condition-0"]')).toHaveAttribute(
    'stroke-dasharray',
    '7 5',
  );
  for (const category of ['Індикатор', 'Умова', 'Логіка', 'Сигнал', 'Результат']) {
    await expect(page.locator(`[data-node-category="${category}"]`).first()).toBeAttached();
  }
  const signalOutput = page.locator('[data-node-id="signal"]').getByRole('button', { name: 'Почати звʼязок' });
  await signalOutput.focus();
  await signalOutput.press('Enter');
  const resultInput = page.locator('[data-node-id="result-0"]').getByRole('button', { name: 'Вхід condition', exact: true });
  await expect(resultInput).toHaveAttribute('aria-description', /Замінити поточний зв’язок/);
  await resultInput.click();
  await expect.poll(() => draft.graph.links.filter((link: any) => link.to === 'result-0').map((link: any) => link.from)).toEqual(['signal']);
  await page.keyboard.press('ControlOrMeta+z');
  await expect.poll(() => draft.graph.links.filter((link: any) => link.to === 'result-0').map((link: any) => link.from)).toEqual(['condition-0']);
  await signalOutput.click();
  await page.keyboard.press('Escape');
  await expect(page.getByRole('button', { name: 'Скасувати', exact: true })).toHaveCount(0);
  await node.scrollIntoViewIfNeeded();
  const box = (await node.boundingBox())!;
  await page.mouse.move(box.x + 70, box.y + 15);
  await page.mouse.down();
  await page.mouse.move(box.x + 160, box.y + 15, { steps: 8 });
  await page.mouse.up();
  await expect.poll(() => draft.layout.positions['condition-0'].x).toBeGreaterThan(32);
  await assertEdgesMeetSockets();
  await openPublishConfirmation(page);
  await expect(page.getByText('Оберіть фонди', { exact: true }).first()).toBeVisible();
  failSave = true;
  await page.getByRole('textbox', { name: 'Назва чернетки' }).fill('Нова назва');
  await page.getByRole('textbox', { name: 'Назва чернетки' }).press('Tab');
  await expect(page.getByRole('button', { name: 'Повторити збереження' })).toBeVisible();
  failSave = false;
  await page.getByRole('button', { name: 'Повторити збереження' }).click();
  await expect(page.getByText('Чернетка · збережено', { exact: true })).toBeVisible();
  for (const width of [1440, 1024, 390]) {
    await page.setViewportSize({ width, height: 900 });
    await page.getByRole('button', { name: 'Кроки', exact: true }).click();
    await expect(page.getByRole('navigation', { name: 'Кроки стратегії' })).toBeVisible();
    expect(
      await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
    ).toBeTruthy();
    await page.screenshot({ path: testInfo.outputPath(`workspace-steps-${width}.png`), fullPage: true });
    if (width < 576) {
      await expect(page.getByRole('button', { name: 'Схема', exact: true })).toHaveCount(0);
      continue;
    }
    await page.getByRole('button', { name: 'Схема', exact: true }).click();
    expect(
      await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
    ).toBeTruthy();
    await assertEdgesMeetSockets();
    await page.screenshot({ path: testInfo.outputPath(`workspace-graph-${width}.png`), fullPage: true });
  }
  conflict = true;
  await page.getByRole('textbox', { name: 'Назва чернетки' }).fill('Локальна незбережена ідея');
  await page.getByRole('textbox', { name: 'Назва чернетки' }).press('Tab');
  await expect(
    page.getByRole('alert').filter({ hasText: 'Ваші зміни залишилися тут' }),
  ).toBeVisible();
  await expect(page.getByRole('textbox', { name: 'Назва чернетки' })).toHaveValue(
    'Локальна незбережена ідея',
  );
  const download = page.waitForEvent('download');
  await page.getByRole('button', { name: 'Завантажити локальну копію' }).click();
  expect((await download).suggestedFilename()).toContain('local-draft.json');
  await openPublishConfirmation(page);
  expect(publishRequests).toBe(0);
  expect(errors).toEqual([]);
});
