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
  await page.getByRole('button', { name: 'Далі' }).click();
  await page.getByRole('button', { name: /Європа 70 \/ США 30/ }).click();
  await expect(page.getByText('US ETF momentum').first()).toBeVisible();
  await shot('03-account-new');
  await page.getByRole('button', { name: 'Далі' }).click();
  await page.locator('input#contribution').fill('227');
  await page.getByRole('button', { name: 'Далі' }).click();
  await expect(page.getByText('Перевірка ризику')).toBeVisible();
  await page.getByRole('button', { name: 'Далі' }).click(); // the reports come at the strategies' time
  await page.getByRole('button', { name: 'Відкрити рахунок' }).click();
  await page.getByRole('button', { name: /Подивитись рахунок/ }).click();
  await page.waitForURL(/\/accounts\/[0-9a-f-]{36}$/);
  await expect(page.getByRole('heading', { name: 'Під-портфелі', exact: true })).toBeVisible();
  await expect(page.locator('input#contribution')).toHaveValue(/227/);
  await shot('04-account-detail');

  await page.goto('/instruments');
  await page.getByRole('button', { name: 'Курси валют' }).click();
  await expectToast(page, 'Курси валют оновлено');
  await shot('05-instruments');

  await page.goto('/reports');
  await shot('06-reports-empty');
  // The buttons are the reports the account's strategies declare: etf-momentum's rebalance is "Ребаланс".
  await page.getByRole('button', { name: 'Ребаланс' }).first().click();
  await page.getByRole('dialog').getByRole('button', { name: 'Згенерувати' }).click();
  await expect(page.getByRole('heading', { name: /Ребаланс ·/ })).toBeVisible({ timeout: 60_000 });
  await expect(page.locator('body')).not.toContainText(/@(report|statement|signal|tax|pit38|etf_momentum|notification)\./);
  await shot('07-report-new');

  const rows = page.locator('p-table tbody tr');
  const contribute = rows.filter({ hasText: 'Внести гроші на рахунок' });
  await contribute.getByRole('button', { name: 'Виконано' }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Відмітити виконання' }).click();
  await expectToast(page, 'Відмічено як виконане');

  const buy = rows.filter({ hasText: /^.*Купити/ }).first();
  const bought = ((await buy.locator('.font-medium').first().textContent()) ?? '').replace('Купити', '').trim();
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

  // A manual sale of part of the bought position → a realisation in the tax year.
  await page.getByRole('button', { name: 'Угода' }).click();
  const trade = page.getByRole('dialog');
  await trade.getByText('Продаж', { exact: true }).click();
  await trade.locator('#trade-instrument').click();
  await page.locator('.p-select-overlay input').fill(bought);
  await page.getByRole('option').first().click();
  await trade.locator('#trade-quantity').fill('0,05');
  await trade.locator('#trade-price').fill('50');
  await trade.locator('#trade-price').blur();
  await trade.getByRole('button', { name: 'Записати' }).click();
  await expectToast(page, 'Угоду записано');

  await page.goto('/taxes');
  await expect(page.getByRole('heading', { name: 'Реалізації (продажі)' })).toBeVisible();
  await expect(page.getByRole('cell', { name: bought }).first()).toBeVisible();
  await shot('11b-taxes');

  await page.goto('/holdings');
  await expect(page.getByRole('heading', { name: 'Європа' })).toBeVisible();
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
