import { expect, Page, test } from '@playwright/test';

async function overlapPairs(page: Page) {
  return page.locator('app-graph-editor article').evaluateAll((elements) => {
    const boxes = elements.map((el) => ({
      id: el.getAttribute('data-node-id'),
      r: el.getBoundingClientRect(),
    }));
    return boxes.flatMap((a, i) =>
      boxes
        .slice(i + 1)
        .filter(
          (b) =>
            Math.min(a.r.right, b.r.right) - Math.max(a.r.left, b.r.left) > 0.5 &&
            Math.min(a.r.bottom, b.r.bottom) - Math.max(a.r.top, b.r.top) > 0.5,
        )
        .map((b) => [a.id, b.id]),
    );
  });
}

test('all actual default templates have no intersecting cards after generation and reload', async ({
  page,
}, testInfo) => {
  test.skip(
    !process.env['E2E_LIVE_TEMPLATES'],
    'Uses a local running API and a disposable test account.',
  );
  await page.setViewportSize({ width: 1366, height: 768 });
  await page.goto('/auth/register');
  await page.locator('#email').fill(`template-layout-${Date.now()}@example.com`);
  await page.locator('#password').fill('layout-test-only-password');
  await page.locator('#confirm').fill('layout-test-only-password');
  await page.getByRole('button', { name: /Зареєструватися/ }).click();
  await expect(page.getByRole('heading', { name: 'Огляд', exact: true })).toBeVisible();
  const vocabularyResponse = await page.request.get('/api/strategy-builder/vocabulary');
  expect(vocabularyResponse.ok()).toBe(true);
  const vocabulary = await vocabularyResponse.json();
  const instruments = await (await page.request.get('/api/strategy-builder/instruments')).json();
  const funds = instruments
    .filter((item: { isVolatilityIndex: boolean }) => !item.isVolatilityIndex)
    .slice(0, 2)
    .map((item: { id: string }) => item.id);
  expect(funds).toHaveLength(2);
  const created: string[] = [];
  try {
    for (const template of vocabulary.templates) {
      const response = await page.request.post('/api/strategy-builder/drafts', {
        data: { name: `Layout ${template.code}`, template: template.code },
      });
      expect(response.ok()).toBe(true);
      let draft = await response.json();
      created.push(draft.id);
      const changed = await page.request.put(`/api/strategy-builder/drafts/${draft.id}`, {
        data: { revision: draft.revision, graph: draft.graph, setup: { ...draft.setup, funds } },
      });
      expect(changed.ok(), await changed.text()).toBe(true);
      draft = await changed.json();
      expect(draft.setup.funds).toEqual(funds);
      await page.goto(`/strategy-builder/${draft.id}`);
      await expect(page.locator('app-graph-editor article')).toHaveCount(draft.graph.nodes.length);
      await expect(page.locator('[data-node-id="strategy-setup"]')).toHaveCount(1);
      await expect(page.locator('[data-node-id="strategy-cash"]')).toHaveCount(1);
      await expect(page.locator('[data-node-id="strategy-portfolio"]')).toHaveCount(1);
      await expect(
        page.locator(
          'path[data-edge-from="result-selection"][data-edge-to="strategy-portfolio"][data-edge-input="funds"]',
        ),
      ).toHaveCount(1);
      await expect(
        page.locator(
          'path[data-edge-from="strategy-cash"][data-edge-to="strategy-portfolio"][data-edge-input="cash"]',
        ),
      ).toHaveCount(1);
      await expect(page.locator('[data-node-id="fund-selection"]')).toHaveCount(1);
      await expect(
        page.locator(
          '[data-node-id="result-entry"] p-inputnumber, [data-node-id="result-exit"] p-inputnumber',
        ),
      ).toHaveCount(0);
      await expect(
        page.locator(
          'path[data-edge-from="fund-selection"][data-edge-to="result-selection"][data-edge-input="funds"]',
        ),
      ).toHaveCount(1);
      await expect(
        page.locator(
          'path[data-edge-from="result-selection"][data-edge-to="strategy-cash"][data-edge-output="cash"]',
        ),
      ).toHaveCount(1);
      await expect.poll(() => overlapPairs(page)).toEqual([]);
      await page.getByRole('button', { name: 'Показати всю схему' }).click();
      await page.screenshot({ path: testInfo.outputPath(`${template.code}.png`) });
      await page.getByRole('button', { name: 'Упорядкувати', exact: true }).click();
      await expect.poll(() => overlapPairs(page)).toEqual([]);
      await page.reload();
      await expect(page.locator('app-graph-editor article')).toHaveCount(draft.graph.nodes.length);
      await expect(page.locator('[data-node-id="strategy-setup"]')).toHaveCount(1);
      await expect.poll(() => overlapPairs(page)).toEqual([]);
    }
  } finally {
    for (const id of created) await page.request.delete(`/api/strategy-builder/drafts/${id}`);
  }
});
