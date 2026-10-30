import { expect, Page, test } from '@playwright/test';

/** A newcomer: registration → guided account setup (balanced preset, the rebalance on payday) → instrument chart → overview. */
test('a newcomer sets up an account with the guided wizard and looks at an instrument', async ({ page }) => {
  const email = `e2e-onboarding-${Date.now()}@example.com`;
  const shot = (name: string) => page.screenshot({ path: `e2e/screenshots/onboarding-${name}.png`, fullPage: true });

  await register(page, email);
  await expect(page.getByRole('button', { name: 'Почати налаштування' })).toBeVisible();
  await shot('01-overview');

  await page.getByRole('button', { name: 'Почати налаштування' }).click();
  await expect(page.getByText('це ваш реальний брокерський рахунок')).toBeVisible();
  await shot('02-account');
  await page.getByRole('button', { name: 'Далі' }).click();

  // Standard strategies exist without asking: the picker shows them in plain words.
  await expect(page.getByText(/найсильніших із \d+ європейських ETF/).first()).toBeVisible({ timeout: 30_000 });
  await page.getByRole('button', { name: /Європа 70 \/ США 30/ }).click();
  await expect(page.getByText('Разом 100%')).toBeVisible();
  await shot('03-split');
  await page.getByRole('button', { name: 'Далі' }).click();

  await page.locator('input#contribution').fill('300');
  await shot('04-contribution');
  await page.getByRole('button', { name: 'Далі' }).click();

  // When the reports come is the user's choice: the rebalance on payday, the 10th at 09:00.
  await expect(page.getByText('Стратегія: щомісяця, 1-го о 20:00')).toBeVisible();
  await page.getByRole('button', { name: 'Свій час' }).first().click();
  await page.locator('input#new-rebalance-day').fill('10');
  await page.locator('input#new-rebalance-time').fill('09:00');
  await shot('05-schedule');
  await page.getByRole('button', { name: 'Далі' }).click();
  await expect(page.getByText('Європа — 70%')).toBeVisible();
  await expect(page.getByText('щомісяця, 10-го о 09:00')).toBeVisible();
  // The review says where each choice can be changed later.
  await expect(page.getByText('Якщо захочете щось змінити потім')).toBeVisible();
  await shot('05-review');
  await page.getByRole('button', { name: 'Відкрити рахунок' }).click();
  await expect(page.getByRole('heading', { name: 'Рахунок готовий' })).toBeVisible();
  await shot('06-done');

  await page.getByRole('button', { name: /Подивитись рахунок/ }).click();
  await expect(page.getByRole('heading', { name: 'Розклад рапортів' })).toBeVisible();
  await expect(page.getByText('Ребаланс').first()).toBeVisible();
  await expect(page.getByText('щомісяця, 10-го о 09:00')).toBeVisible();
  await expect(page.getByText(/Ваш час · стратегія: щомісяця, 1-го о 20:00/)).toBeVisible();
  await shot('07-account');

  await page.goto('/instruments');
  await page.getByRole('button', { name: /NQSE/ }).first().click();
  await expect(page.getByText('Волатильність')).toBeVisible({ timeout: 20_000 });
  await shot('08-instrument-detail');

  await page.goto('/strategies');
  await expect(page.getByText(/Працює в:/).first()).toBeVisible();
  await shot('09-strategies');
});

async function register(page: Page, email: string): Promise<void> {
  await page.goto('/auth/register');
  await page.locator('#email').fill(email);
  await page.locator('#password').fill('correct horse battery');
  await page.locator('#confirm').fill('correct horse battery');
  await page.getByRole('button', { name: 'Зареєструватися' }).click();
  await expect(page.getByRole('heading', { name: 'Огляд' })).toBeVisible();
}
