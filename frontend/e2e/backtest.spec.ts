import { expect, test } from '@playwright/test';

/** A strategy shows how it behaved on history, and what that would have meant in money. */
test('a strategy shows its backtest and the money outcomes', async ({ page }) => {
  test.setTimeout(15 * 60_000);
  const email = `e2e-backtest-${Date.now()}@example.com`;

  await page.goto('/auth/register');
  await page.locator('#email').fill(email);
  await page.locator('#password').fill('correct horse battery');
  await page.locator('#confirm').fill('correct horse battery');
  await page.getByRole('button', { name: 'Зареєструватися' }).click();
  await expect(page.getByRole('heading', { name: 'Огляд' })).toBeVisible();

  for (const name of ['EU index hold', 'EU adaptive momentum']) {
    await page.goto('/strategies');
    await page.getByText(name).first().click();
    await expect(page.getByRole('heading', { name: 'Як стратегія поводилась у минулому' })).toBeVisible();

    const started = Date.now();
    await expect(page.getByText('Зростання 100 одиниць')).toBeVisible({ timeout: 14 * 60_000 });
    console.log(`${name}: result after ${Math.round((Date.now() - started) / 1000)} s`);

    await expect(page.getByText('Найбільше просідання')).toBeVisible();
    await expect(page.getByText('Що було б із вашими грошима')).toBeVisible();
    await page.screenshot({ path: `e2e/screenshots/backtest-${name.replaceAll(' ', '-')}.png`, fullPage: true });
  }
});
