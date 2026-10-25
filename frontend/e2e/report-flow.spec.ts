import { expect, Page, test } from '@playwright/test';

/**
 * Stage 6 criterion: sign in → account → report → partial fill → the trade appears in the journal.
 * Every screen is captured into e2e/screenshots for a visual check.
 */
test('a new user goes from registration to a partly executed monthly report', async ({ page }) => {
  const email = `e2e-${Date.now()}@example.com`;
  const shot = (name: string) => page.screenshot({ path: `e2e/screenshots/${name}.png`, fullPage: true });

  await page.goto('/auth/register');
  await shot('01-register');
  await page.locator('#email').fill(email);
  await page.locator('#password').fill('correct horse battery');
  await page.locator('#confirm').fill('correct horse battery');
  await page.getByRole('button', { name: 'Зареєструватися' }).click();
  await expect(page.getByRole('heading', { name: 'Огляд' })).toBeVisible();
  await shot('02-overview-onboarding');

  await page.goto('/accounts/new');
  await page.getByRole('button', { name: /Як у Python-флоу/ }).click();
  await expect(page.getByText('US ETF momentum')).toBeVisible();
  await shot('03-account-new');
  await page.getByRole('button', { name: 'Відкрити рахунок' }).click();
  await page.waitForURL(/\/accounts\/[0-9a-f-]{36}$/);
  await expect(page.getByRole('heading', { name: 'Під-портфелі', exact: true })).toBeVisible();
  await expect(page.locator('input#contribution')).toHaveValue(/227/);
  await shot('04-account-detail');

  await page.goto('/instruments');
  await page.getByRole('button', { name: 'Курси NBP' }).click();
  await expectToast(page, 'Курси NBP оновлено');
  await shot('05-instruments');

  await page.goto('/reports');
  await shot('06-reports-empty');
  await page.getByRole('button', { name: 'Згенерувати рапорт' }).first().click();
  await page.getByRole('dialog').getByRole('button', { name: 'Згенерувати' }).click();
  await expect(page.getByRole('heading', { name: /Рапорт за/ })).toBeVisible({ timeout: 60_000 });
  await shot('07-report-new');

  const rows = page.locator('p-table tbody tr');
  const contribute = rows.filter({ hasText: 'Внести гроші на рахунок' });
  await contribute.getByRole('button', { name: 'Виконано' }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Відмітити виконання' }).click();
  await expectToast(page, 'Відмічено як виконане');

  const buy = rows.filter({ hasText: /^.*Купити/ }).first();
  await buy.locator('.p-splitbutton-dropdown').click();
  await page.getByRole('menuitem', { name: 'Частково виконано' }).click();
  const dialog = page.getByRole('dialog');
  await dialog.locator('#quantity').fill('0,1');
  await dialog.locator('#quantity').blur();
  await shot('08-partial-fill-dialog');
  await dialog.getByRole('button', { name: 'Зберегти частину' }).click();
  await expectToast(page, 'Часткове виконання збережено');
  await expect(buy.getByText(/з .* шт\./)).toBeVisible();
  await shot('09-report-in-progress');

  await page.getByRole('tab').nth(0).click();
  await page.mouse.wheel(0, 2000);
  await shot('10-report-rationale');

  await page.goto('/transactions');
  await expect(page.getByText('Внесок', { exact: true }).first()).toBeVisible();
  await expect(page.getByText('Купівля', { exact: true }).first()).toBeVisible();
  await shot('11-journal');

  await page.goto('/holdings');
  await expect(page.getByRole('heading', { name: 'EU momentum' })).toBeVisible();
  await shot('12-holdings');

  await page.goto('/strategies');
  await shot('13-strategies');
  await page.getByText('EU ETF momentum').first().click();
  await expect(page.getByRole('heading', { name: 'Параметри' })).toBeVisible();
  await shot('14-strategy-detail');

  await page.goto('/');
  await shot('15-overview');
});

async function expectToast(page: Page, text: string): Promise<void> {
  await expect(page.locator('.p-toast').getByText(text).first()).toBeVisible();
}
