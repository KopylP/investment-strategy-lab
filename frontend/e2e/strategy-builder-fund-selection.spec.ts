import { expect, test } from '@playwright/test';

test('selecting all grouped funds saves instrument identifiers and survives reload', async ({
  page,
}) => {
  await page.goto('/auth/register');
  await page.locator('#email').fill(`fund-selection-${Date.now()}@example.com`);
  await page.locator('#password').fill('correct horse battery');
  await page.locator('#confirm').fill('correct horse battery');
  await page.getByRole('button', { name: 'Зареєструватися' }).click();
  await expect(page.getByRole('heading', { name: 'Огляд', exact: true })).toBeVisible();

  const directoryResponse = await page.request.get('/api/strategy-builder/instruments');
  expect(directoryResponse.ok()).toBeTruthy();
  const directory = await directoryResponse.json();
  const funds = directory
    .filter((instrument: { isVolatilityIndex: boolean }) => !instrument.isVolatilityIndex)
    .slice(0, 3)
    .map((instrument: { id: string }, index: number) => ({
      ...instrument,
      group: index < 2 ? 'Перша група' : 'Друга група',
    }));
  expect(funds).toHaveLength(3);
  const expectedIds = funds.map((fund: { id: string }) => fund.id).sort();
  await page.route('**/api/strategy-builder/instruments', (route) =>
    route.fulfill({ json: funds }),
  );

  const created = await page.request.post('/api/strategy-builder/drafts', {
    data: { name: 'Вибір усіх фондів', template: null },
  });
  expect(created.ok()).toBeTruthy();
  const draft = await created.json();

  try {
    await page.goto(`/strategy-builder/${draft.id}`);
    await page.getByRole('button', { name: '2. Фонди й ряди', exact: true }).click();
    await page.locator('p-multiselect:has(#builder-funds)').click();
    const savedResponse = page.waitForResponse(
      (response) =>
        response.url().endsWith(`/drafts/${draft.id}`) && response.request().method() === 'PUT',
    );
    await page.locator('.p-multiselect-header').getByRole('checkbox').check();
    const saved = await savedResponse;
    const selectedIds = saved.request().postDataJSON().setup.funds;
    expect(selectedIds.toSorted()).toEqual(expectedIds);
    expect(saved.ok()).toBeTruthy();
    await page.keyboard.press('Escape');
    await expect(page.getByText('3 із 60', { exact: true })).toBeVisible();
    await expect(page.getByText('Чернетка · збережено', { exact: true })).toBeVisible();

    await page.reload();
    await page.getByRole('button', { name: '2. Фонди й ряди', exact: true }).click();
    await expect(page.getByText('3 із 60', { exact: true })).toBeVisible();
    const persisted = await (
      await page.request.get(`/api/strategy-builder/drafts/${draft.id}`)
    ).json();
    expect(persisted.setup.funds.toSorted()).toEqual(expectedIds);
  } finally {
    await page.request.delete(`/api/strategy-builder/drafts/${draft.id}`);
  }
});
