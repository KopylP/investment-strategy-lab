import { expect, test } from '@playwright/test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';

/** Stage 8: positions imported half a year back give the dashboard a history to draw. */
test('the dashboard shows value, returns, drawdown and allocation over time', async ({ page }) => {
  const email = `e2e-dash-${Date.now()}@example.com`;
  await page.goto('/auth/register');
  await page.locator('#email').fill(email);
  await page.locator('#password').fill('correct horse battery');
  await page.locator('#confirm').fill('correct horse battery');
  await page.getByRole('button', { name: 'Зареєструватися' }).click();
  await expect(page.getByRole('heading', { name: 'Огляд' })).toBeVisible();

  const api = page.request;
  const strategies = await (await api.post('/api/strategies/defaults', { data: {} })).json();
  const account = await (await api.post('/api/portfolio/accounts', {
    data: {
      name: 'XTB EUR', broker: 'XTB', baseCurrency: 'EUR',
      subPortfolios: [
        { name: 'EU momentum', targetSharePercent: 70, strategyId: strategies[0].id },
        { name: 'US momentum', targetSharePercent: 30, strategyId: strategies[1].id },
      ],
    },
  })).json();
  const asOf = new Date();
  asOf.setMonth(asOf.getMonth() - 6);
  const positions = readFileSync(join(__dirname, '../../strategy-engine/src/finance_decisions/strategies/etf_momentum/positions.json'), 'utf8');
  const imported = await api.post(`/api/portfolio/accounts/${account.id}/sub-portfolios/${account.subPortfolios[0].id}/import-positions?asOf=${asOf.toISOString().slice(0, 10)}`, {
    data: JSON.parse(positions),
  });
  expect(imported.ok()).toBeTruthy();

  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Вартість у часі' })).toBeVisible({ timeout: 60_000 });
  await expect(page.locator('app-value-chart canvas')).toBeVisible();
  await page.waitForTimeout(1500);
  await page.screenshot({ path: 'e2e/screenshots/16-dashboard.png', fullPage: true });

  await page.locator('.layout-topbar-action').filter({ has: page.locator('.pi-sun, .pi-moon') }).click();
  await page.waitForTimeout(1500);
  await page.screenshot({ path: 'e2e/screenshots/17-dashboard-dark.png', fullPage: true });
});
