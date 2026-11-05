import { expect, Page, test } from '@playwright/test';
import { mockStrategyWorkspace } from './support/strategy-workspace';

async function listFixture(page: Page) {
  let deleted = false,
    fail = false;
  const deletedPaths: string[] = [];
  const row = {
    id: 'workspace',
    name: 'Чернетка опублікованої',
    originStrategyName: 'Опублікована стратегія',
    originVersion: 3,
    updatedAt: new Date().toISOString(),
    lastTrialAt: null,
  };
  await mockStrategyWorkspace(page);
  await page.route('**/api/strategy-builder/drafts', (route) =>
    route.fulfill({
      json: deleted
        ? [{ ...row, id: 'other', name: 'Інша чернетка' }]
        : [row, { ...row, id: 'other', name: 'Інша чернетка' }],
    }),
  );
  await page.route('**/api/strategy-builder/drafts/workspace', async (route) => {
    if (route.request().method() !== 'DELETE') return route.fallback();
    deletedPaths.push(new URL(route.request().url()).pathname);
    if (fail) return route.fulfill({ status: 500, json: { detail: 'Тимчасова помилка' } });
    deleted = true;
    await route.fulfill({ status: 204 });
  });
  return { deletedPaths, setFail: (value: boolean) => (fail = value) };
}

for (const width of [1440, 390]) {
  test(`discard from list preserves published strategies at width ${width}`, async ({ page }) => {
    await page.setViewportSize({ width, height: 900 });
    const state = await listFixture(page);
    await page.goto('/strategy-builder');
    await expect(page.getByRole('link', { name: '«Стратегії»', exact: true })).toHaveAttribute(
      'href',
      '/strategies',
    );
    const remove = () =>
      page.getByRole('button', { name: 'Видалити чернетку', exact: true }).first();
    await remove().click();
    const dialog = page.getByRole('alertdialog', {name:'Видалити чернетку?',exact:true});
    await expect(dialog).toContainText('повністю видалено без можливості відновлення');
    await expect(dialog).toContainText('Видалення чернетки не впливає на них');
    await dialog.getByRole('button', { name: 'Скасувати', exact: true }).click();
    expect(state.deletedPaths).toEqual([]);
    await expect(page).toHaveURL(/\/strategy-builder$/);
    state.setFail(true);
    await remove().click();
    await dialog.getByRole('button', { name: 'Видалити', exact: true }).click();
    await expect(page.getByText('Не вдалося видалити чернетку', { exact: true })).toBeVisible();
    await expect(page.getByText('Чернетка опублікованої', { exact: true })).toBeVisible();
    state.setFail(false);
    await remove().click();
    await dialog.getByRole('button', { name: 'Видалити', exact: true }).click();
    await expect(page.getByText('Чернетка опублікованої', { exact: true })).toHaveCount(0);
    await expect(page.getByText('Інша чернетка', { exact: true })).toBeVisible();
    expect(state.deletedPaths).toEqual([
      '/api/strategy-builder/drafts/workspace',
      '/api/strategy-builder/drafts/workspace',
    ]);
  });
}

test('discard from editor waits for an in-flight save and cancels queued autosaves', async ({
  page,
}) => {
  const fixture = await mockStrategyWorkspace(page);
  let releaseSave!: () => void;
  const pendingSave = new Promise<void>((resolve) => (releaseSave = resolve));
  const events: string[] = [];
  await page.route('**/api/strategy-builder/drafts/workspace', async (route) => {
    const method = route.request().method();
    if (method === 'PUT') {
      events.push('save-start');
      await pendingSave;
      events.push('save-end');
      return route.fulfill({
        json: { ...fixture.draft, ...route.request().postDataJSON(), revision: 2 },
      });
    }
    if (method === 'DELETE') {
      events.push('delete');
      return route.fulfill({ status: 204 });
    }
    return route.fallback();
  });
  await page.goto('/strategy-builder/workspace');
  const name = page.getByRole('textbox', { name: 'Назва чернетки' });
  await name.fill('Зміна перед видаленням');
  await name.press('Tab');
  await expect.poll(() => events).toEqual(['save-start']);
  await page.getByRole('button', { name: 'Видалити чернетку', exact: true }).click();
  await page
    .getByRole('alertdialog')
    .getByRole('button', { name: 'Видалити', exact: true })
    .click();
  expect(events).toEqual(['save-start']);
  releaseSave();
  await expect(page).toHaveURL(/\/strategy-builder$/);
  await expect.poll(() => events).toEqual(['save-start', 'save-end', 'delete']);
  await page.waitForTimeout(1700);
  expect(events).toEqual(['save-start', 'save-end', 'delete']);
});
