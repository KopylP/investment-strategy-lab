import { readFile } from 'node:fs/promises';
import { expect, test } from '@playwright/test';

test('guided edits persist across modes and problems select their graph node', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', (error) => errors.push(error.message));
  await page.goto('/auth/register');
  await page.locator('#email').fill(`e2e-builder-${Date.now()}@example.com`);
  await page.locator('#password').fill('correct horse battery');
  await page.locator('#confirm').fill('correct horse battery');
  await page.getByRole('button', { name: 'Зареєструватися' }).click();
  await expect(page.getByRole('heading', { name: 'Огляд', exact: true })).toBeVisible();

  const vocabularyResponse = await page.request.get('/api/strategy-builder/vocabulary');
  expect(vocabularyResponse.ok()).toBeTruthy();
  const vocabulary = await vocabularyResponse.json();
  const created = await page.request.post('/api/strategy-builder/drafts', {
    data: { name: 'Перевірка редактора', template: vocabulary.templates[0].code },
  });
  expect(created.ok()).toBeTruthy();
  let draft = await created.json();
  const instruments = await (await page.request.get('/api/strategy-builder/instruments')).json();
  const regular = instruments.filter(
    (instrument: { isVolatilityIndex: boolean }) => !instrument.isVolatilityIndex,
  );
  const configured = await page.request.put(`/api/strategy-builder/drafts/${draft.id}`, {
    data: {
      revision: draft.revision,
      setup: {
        funds: regular.slice(0, 5).map((instrument: { id: string }) => instrument.id),
        benchmarkId: regular[0].id,
        cashId: regular[5].id,
        volatilityIndexId: null,
        extraSeries: [],
      },
    },
  });
  expect(configured.ok()).toBeTruthy();
  draft = await configured.json();
  await page.goto(`/strategy-builder/${draft.id}`);
  await page.getByRole('button', { name: 'Кроки', exact: true }).click();
  await page.getByRole('button', { name: '6. Відбір і ваги', exact: true }).click();
  const inspector = page.locator('app-node-inspector').first();
  await expect(inspector).toBeVisible();
  const numeric = inspector.locator('input[role="spinbutton"]').first();
  await numeric.fill('2');
  await numeric.press('Tab');
  await expect
    .poll(async () => (await (await page.request.get(`/api/strategy-builder/drafts/${draft.id}`)).json()).revision)
    .toBeGreaterThan(draft.revision);
  await page.getByRole('button', { name: 'Схема', exact: true }).click();
  await expect(page.locator('app-graph-editor')).toBeVisible();
  await expect(page.locator('app-node-inspector').last()).toBeVisible();
  await page.screenshot({ path: 'test-results/builder-graph.png', fullPage: true });
  await page.reload();
  const saved = await (await page.request.get(`/api/strategy-builder/drafts/${draft.id}`)).json();
  expect(saved.revision).toBeGreaterThan(draft.revision);

  await page.locator('.review-button').click();
  await expect(page.locator('app-builder-words-panel')).toBeVisible();
  await expect(page.locator('app-builder-words-panel')).not.toContainText('@rule_based.');
  await page.getByRole('button', { name: 'JSON', exact: true }).click();
  const exportButton = page.getByRole('button', { name: /Експортувати/ });
  await expect(exportButton).toBeEnabled();
  const downloadReady = page.waitForEvent('download');
  await exportButton.click();
  const download = await downloadReady;
  expect(download.suggestedFilename()).toBe('Перевірка редактора.json');
  const exported = JSON.parse(await readFile((await download.path())!, 'utf8'));
  await page.screenshot({ path: 'test-results/builder-json.png', fullPage: true });

  const entryCompare = saved.graph.nodes.find((node: { id: string }) => node.id === 'e-mom-positive');
  const brokenGraph = {
    ...saved.graph,
    links: saved.graph.links.filter(
      (link: { to: string; input: string }) => link.to !== entryCompare.id || link.input !== 'left',
    ),
  };
  const brokenResponse = await page.request.put(`/api/strategy-builder/drafts/${draft.id}`, {
    data: { revision: saved.revision, graph: brokenGraph },
  });
  expect(brokenResponse.ok()).toBeTruthy();
  const broken = await brokenResponse.json();
  const problem = broken.problems.find(
    (item: { nodeId: string | null }) => item.nodeId === entryCompare.id,
  );
  expect(problem).toBeTruthy();
  await page.reload();
  await page.locator('.review-button').click();
  await page
    .locator('app-builder-problems-panel')
    .getByRole('button')
    .nth(broken.problems.indexOf(problem))
    .click();
  await expect(page.locator('app-graph-editor article.selected')).toBeVisible();
  await expect(page.locator('app-graph-editor article.selected')).toContainText('Під\'єднайте вхід');
  await page.setViewportSize({ width: 1024, height: 900 });
  await page.getByRole('button', { name: 'Кроки', exact: true }).click();
  await page.screenshot({ path: 'test-results/builder-steps-1024.png', fullPage: true });
  const width = await page.evaluate(() => ({
    scroll: document.documentElement.scrollWidth,
    viewport: innerWidth,
  }));
  expect(width.scroll).toBeLessThanOrEqual(width.viewport);
  const outgoing = saved.graph.links.find((link: { from: string }) => link.from === entryCompare.id);
  const nestedGraph = {
    nodes: [...saved.graph.nodes, { id: 'nested-not', kind: 'Not', block: 'Entry', parameters: {} }],
    links: [
      ...saved.graph.links.filter((link: { from: string; to: string }) => link.from !== outgoing.from || link.to !== outgoing.to),
      { from: outgoing.from, to: 'nested-not', input: 'condition' },
      { from: 'nested-not', to: outgoing.to, input: outgoing.input },
    ],
  };
  const nested = await page.request.put(`/api/strategy-builder/drafts/${draft.id}`, {
    data: { revision: broken.revision, graph: nestedGraph },
  });
  expect(nested.ok()).toBeTruthy();
  await page.reload();
  await page.getByRole('button', { name: 'Кроки', exact: true }).click();
  await page.getByRole('button', { name: '5. Вхід', exact: true }).click();
  await expect(page.locator('app-readonly-condition')).toBeVisible();
  await expect(page.locator('app-condition-rows')).toHaveCount(0);
  await page.getByRole('button', { name: 'JSON', exact: true }).click();
  const unknown = structuredClone(exported);
  unknown.selection.foo = 1;
  await page.locator('app-builder-json-view input[type="file"]').setInputFiles({
    name: 'unknown.json',
    mimeType: 'application/json',
    buffer: Buffer.from(JSON.stringify(unknown)),
  });
  await expect(page.locator('app-builder-json-view [role="alert"]')).toContainText('selection.foo');
  const outOfRange = structuredClone(exported);
  outOfRange.selection.top = 99;
  await page.locator('app-builder-json-view input[type="file"]').setInputFiles({
    name: 'out-of-range.json',
    mimeType: 'application/json',
    buffer: Buffer.from(JSON.stringify(outOfRange)),
  });
  await expect(page).not.toHaveURL(new RegExp(draft.id));
  await page.locator('.review-button').click();
  await expect(page.locator('app-builder-problems-panel button').first()).toBeVisible();
  expect(errors).toEqual([]);
});
