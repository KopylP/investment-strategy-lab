import { expect, test } from '@playwright/test';
import { openPublishConfirmation } from './support/editor-menu';

test('new strategy can be configured, saved, published and revised entirely through the UI', async ({ page }, testInfo) => {
  const errors: string[] = [];
  const failedRequests: string[] = [];
  page.on('pageerror', error => errors.push(error.message));
  page.on('response', response => {
    if (response.url().includes('/api/strategy-builder/') && response.status() >= 500)
      failedRequests.push(`${response.status()} ${response.url()}`);
  });
  await page.goto('/auth/register');
  await page.locator('#email').fill(`builder-live-${Date.now()}@example.com`);
  await page.locator('#password').fill('correct horse battery');
  await page.locator('#confirm').fill('correct horse battery');
  await page.getByRole('button', { name: 'Зареєструватися' }).click();
  await expect(page.getByRole('heading', { name: 'Огляд', exact: true })).toBeVisible();
  await page.goto('/strategy-builder');
  await page.getByRole('button', { name: 'Нова стратегія' }).first().click();
  await page.locator('p-select:has(#builder-template)').click();
  await page.getByRole('option', { name: 'Моментум із фільтром тренду', exact: true }).click();
  await page.locator('#builder-draft-name').fill(`UI publication ${Date.now()}`);
  await page.getByRole('button', { name: 'Створити' }).click();
  await expect(page).toHaveURL(/\/strategy-builder\/[\w-]+$/);
  const draftId = page.url().split('/').at(-1)!;
  await page.getByRole('button', { name: 'Кроки', exact: true }).click();
  await page.getByRole('button', { name: '2. Фонди й ряди', exact: true }).click();
  await page.getByRole('button', { name: 'Обрати фонди' }).click();
  const fundDialog = page.getByRole('dialog', { name: 'Фонди стратегії' });
  const fundChoices = fundDialog.locator('.fund-row input');
  await expect(fundChoices.first()).toBeVisible();
  for (let index = 0; index < 3; index++) await fundChoices.nth(index).check();
  await fundDialog.getByRole('button', { name: /^Застосувати/ }).click();
  await expect(fundDialog).toBeHidden();
  await page.locator('p-select:has(#builder-benchmark)').click();
  await page.getByRole('option').first().click();
  await page.locator('p-select:has(#builder-cash)').click();
  await page.getByRole('option').nth(3).click();
  await expect
    .poll(async () => {
      const current = await (await page.request.get(`/api/strategy-builder/drafts/${draftId}`)).json();
      return [current.setup.funds.length, !!current.setup.benchmarkId, !!current.setup.cashId];
    })
    .toEqual([3, true, true]);
  await page.reload();
  await page.getByRole('button', { name: 'Кроки', exact: true }).click();
  await page.getByRole('button', { name: '2. Фонди й ряди', exact: true }).click();
  await expect(page.getByText('3 із 60', { exact: true })).toBeVisible();
  const saved = await (await page.request.get(`/api/strategy-builder/drafts/${draftId}`)).json();
  expect(saved.setup.funds).toHaveLength(3);
  expect(saved.setup.benchmarkId).toBeTruthy();
  expect(saved.setup.cashId).toBeTruthy();
  await page.getByRole('button', { name: 'Схема', exact: true }).click();
  await expect(page.locator('[data-block-canvas]')).toHaveCount(1);
  await page.screenshot({ path: testInfo.outputPath('real-strategy-graph.png'), fullPage: true });
  await page.getByRole('button', { name: 'Кроки', exact: true }).click();
  await page.getByRole('button', { name: '9. Перевірка й бектест', exact: true }).click();
  await page.getByRole('button', { name: 'Перевірити' }).click();
  const publish = async () => {
    await openPublishConfirmation(page);
    const confirmation = page.getByRole('alertdialog', { name: 'Публікація стратегії' });
    await expect(confirmation).toBeVisible();
    await confirmation.getByRole('button', { name: 'Опублікувати', exact: true }).click();
    await expect(page).toHaveURL(/\/strategies\/[\w-]+$/);
  };
  await publish();
  const publishedUrl = page.url();
  await page.screenshot({ path: testInfo.outputPath('published-strategy.png'), fullPage: true });
  expect((await page.request.get(`/api/strategy-builder/drafts/${draftId}`)).status()).toBe(404);
  await page.reload();
  await page.getByRole('button', { name: 'Змінити в конструкторі' }).click();
  await expect(page).toHaveURL(/\/strategy-builder\/[\w-]+$/);
  await publish();
  expect(page.url()).toBe(publishedUrl);
  await expect(page.getByRole('combobox').first()).toContainText('Версія 2');
  expect(failedRequests).toEqual([]);
  expect(errors).toEqual([]);
});
