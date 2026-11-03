import { expect, test } from '@playwright/test';

test('funds found at the provider join the draft and remain selected after reload', async ({
  page,
}) => {
  const errors: string[] = [];
  page.on('pageerror', (error) => errors.push(error.message));
  await page.goto('/auth/register');
  await page.locator('#email').fill(`fund-search-${Date.now()}@example.com`);
  await page.locator('#password').fill('correct horse battery');
  await page.locator('#confirm').fill('correct horse battery');
  await page.getByRole('button', { name: 'Зареєструватися' }).click();
  await expect(page.getByRole('heading', { name: 'Огляд', exact: true })).toBeVisible();

  const created = await page.request.post('/api/strategy-builder/drafts', {
    data: { name: 'Пошук фондів', template: null },
  });
  expect(created.ok()).toBeTruthy();
  const draft = await created.json();
  await page.goto(`/strategy-builder/${draft.id}`);
  await page.getByRole('button', { name: '2. Фонди й ряди', exact: true }).click();
  await page.getByRole('button', { name: 'Знайти фонди', exact: true }).click();
  const dialog = page.getByRole('dialog', { name: 'Знайти фонди', exact: true });
  await dialog.locator('#fund-search-exchange').click();
  await page.getByRole('option', { name: 'Xetra', exact: true }).click();
  await dialog.locator('#fund-search-assets').fill('1000000000');
  await dialog.locator('#fund-search-words').fill('MSCI World');
  await dialog.getByRole('button', { name: 'Шукати', exact: true }).click();
  const rows = dialog.locator('tbody tr');
  await expect(rows.first().getByRole('checkbox')).toBeVisible({ timeout: 60_000 });
  expect(await rows.count()).toBeGreaterThanOrEqual(3);
  for (let index = 0; index < 3; index++) await rows.nth(index).getByRole('checkbox').check();
  await dialog.getByRole('button', { name: 'Додати вибрані', exact: true }).click();
  await expect(dialog).toBeHidden({ timeout: 60_000 });
  await expect(page.getByText('3 із 60', { exact: true })).toBeVisible();
  await expect(page.getByText('Чернетка · збережено', { exact: true })).toBeVisible();

  const saved = await (await page.request.get(`/api/strategy-builder/drafts/${draft.id}`)).json();
  expect(saved.setup.funds).toHaveLength(3);
  const directory = await (await page.request.get('/api/strategy-builder/instruments')).json();
  for (const id of saved.setup.funds)
    expect(directory.some((fund: { id: string }) => fund.id === id)).toBeTruthy();

  await page.reload();
  await page.getByRole('button', { name: '2. Фонди й ряди', exact: true }).click();
  await expect(page.getByText('3 із 60', { exact: true })).toBeVisible();
  await page.setViewportSize({ width: 390, height: 844 });
  await page.getByRole('button', { name: 'Знайти фонди', exact: true }).click();
  await expect(dialog).toBeVisible();
  expect(
    await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth),
  ).toBeTruthy();
  await page.screenshot({ path: 'test-results/fund-search-phone.png', fullPage: true });

  await page.route('**/api/strategy-builder/funds/search', (route) =>
    route.fulfill({
      status: 409,
      contentType: 'application/problem+json',
      body: JSON.stringify({
        code: 'market_data.fund_screen.unavailable',
        detail: 'Fund search is unavailable.',
      }),
    }),
  );
  await dialog.getByRole('button', { name: 'Шукати', exact: true }).click();
  await expect(dialog.getByText(/Пошук зараз недоступний/)).toBeVisible();
  await dialog.getByRole('button', { name: 'Закрити', exact: true }).click();
  await expect(page.getByText('3 із 60', { exact: true })).toBeVisible();
  expect(errors).toEqual([]);
});
