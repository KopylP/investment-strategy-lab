import { expect, Page, test } from '@playwright/test';
import { defaultStrategies } from './default-strategies';

const screenshotsDir = process.env['SIGNALS_SCREENSHOTS'] ?? 'e2e/screenshots';

async function registerWithAccount(page: Page): Promise<void> {
  await page.goto('/auth/register');
  await page.locator('#email').fill(`e2e-signals-${Date.now()}@example.com`);
  await page.locator('#password').fill('correct horse battery');
  await page.locator('#confirm').fill('correct horse battery');
  await page.getByRole('button', { name: 'Зареєструватися' }).click();
  await expect(page.getByRole('heading', { name: 'Огляд' })).toBeVisible();

  const [strategy] = await defaultStrategies(page);

  const account = await page.request.post('/api/portfolio/accounts', {
    data: {
      name: 'XTB EUR',
      broker: 'XTB',
      baseCurrency: 'EUR',
      subPortfolios: [{ name: 'EU momentum', targetSharePercent: 100, strategyId: strategy.id }],
    },
  });

  expect(account.ok()).toBeTruthy();
}

test('built-in signals appear, a new one is created and can start a report', async ({ page }) => {
  await registerWithAccount(page);

  await page.goto('/signals');
  await expect(page.getByRole('heading', { name: 'Зараз спрацювали' })).toBeVisible();

  await expect(async () => {
    await page.reload();
    await expect(page.getByText('Просадка рахунку «XTB EUR»')).toBeVisible({ timeout: 2_000 });
  }).toPass({ timeout: 90_000 });
  await expect(page.getByText('Високий VIX')).toBeVisible();
  await page.screenshot({ path: `${screenshotsDir}/signals-page.png`, fullPage: true });

  await page.getByRole('button', { name: 'Новий сигнал' }).click();
  await page.locator('#signal-name').fill('Падіння за день від 3%');
  await page.locator('#signal-metric').click();
  await page.getByRole('option', { name: 'Падіння за день' }).click();
  await page.locator('#signal-threshold').fill('3');
  await page.screenshot({ path: `${screenshotsDir}/signal-new.png` });
  await page.getByRole('button', { name: 'Зберегти' }).click();
  await expect(page.getByText('Сигнал створено')).toBeVisible();

  await page.getByRole('button', { name: /Падіння за день від 3%/ }).click();
  await expect(page.getByRole('heading', { name: 'Рапорти, які запускає сигнал' })).toBeVisible();
  await page.getByRole('combobox', { name: 'Рапорт' }).click();
  await page.getByRole('option').first().click();
  await page.getByRole('button', { name: 'Під’єднати' }).click();
  await expect(page.getByText('Рапорт під’єднано')).toBeVisible();
  await page.screenshot({ path: `${screenshotsDir}/signal-edit.png`, fullPage: true });

  await page.getByRole('switch', { name: 'Надсилати сповіщення' }).click();
  await expect(page.getByText('Сповіщення вимкнено')).toBeVisible();
  await page.getByRole('button', { name: 'Скасувати' }).click();

  await page.goto('/profile');
  await expect(page.getByRole('heading', { name: 'Про які сигнали сповіщати' })).toBeVisible();
  await expect(page.getByRole('checkbox', { name: 'Падіння за день від 3%' })).not.toBeChecked();
  await page.screenshot({ path: `${screenshotsDir}/signals-profile.png`, fullPage: true });

  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/signals');
  await expect(page.getByRole('heading', { name: 'Мої сигнали' })).toBeVisible();
  await page.screenshot({ path: `${screenshotsDir}/signals-phone.png`, fullPage: true });
});
