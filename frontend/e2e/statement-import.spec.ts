import { expect, test } from '@playwright/test';

/** Stage 10: a (synthetic) Revolut statement through the wizard: mapping → preview → import → undo. */
const statement = [
  'Date,Ticker,Type,Quantity,Price per share,Total Amount,Currency,FX Rate',
  '2026-06-01T09:00:00.100Z,,CASH TOP-UP,,,EUR 600,EUR,0.2340',
  '2026-06-02T10:15:30.200Z,QDVE,BUY - MARKET,3,EUR 40,EUR 120,EUR,0.2340',
  '2026-06-03T15:30:00.300Z,AAPL,BUY - MARKET,0.5,USD 210,USD 105,USD,0.2564',
  '2026-06-10T12:00:00.400Z,AAPL,DIVIDEND,,,USD 0.12,USD,0.2564',
  '2026-06-21T15:00:00.700Z,,CUSTODY FEE,,,EUR -0.50,EUR,0.2343',
].join('\n');

test('a Revolut statement is mapped, previewed, imported and undone', async ({ page }) => {
  await page.goto('/auth/register');
  await page.locator('#email').fill(`e2e-import-${Date.now()}@example.com`);
  await page.locator('#password').fill('correct horse battery');
  await page.locator('#confirm').fill('correct horse battery');
  await page.getByRole('button', { name: 'Зареєструватися' }).click();
  await expect(page.getByRole('heading', { name: 'Огляд' })).toBeVisible();

  const api = page.request;
  await api.post('/api/market-data/fx-rates/ingest');
  const strategies = await (await api.post('/api/strategies/defaults', { data: {} })).json();
  await api.post('/api/portfolio/accounts', {
    data: {
      name: 'Revolut', broker: 'Revolut', baseCurrency: 'EUR',
      subPortfolios: [
        { name: 'EU momentum', targetSharePercent: 70, strategyId: strategies[0].id },
        { name: 'Акції', targetSharePercent: 30 },
      ],
    },
  });

  await page.goto('/transactions/import');
  await page.locator('input[type=file]').setInputFiles({ name: 'revolut.csv', mimeType: 'text/csv', buffer: Buffer.from(statement) });
  await expect(page.getByRole('heading', { name: '1. Інструменти й під-портфелі' })).toBeVisible();
  await page.screenshot({ path: 'e2e/screenshots/18-import-mapping.png', fullPage: true });

  // QDVE is in the EU strategy's universe → its sleeve is suggested; AAPL is new and goes to "Акції".
  const apple = page.locator('tr', { hasText: 'AAPL' }).first();
  await apple.locator('p-select').nth(1).click();
  await page.getByRole('option', { name: 'Акції' }).click();
  await expect(page.getByText('без зіставлення')).toHaveCount(0);
  await page.screenshot({ path: 'e2e/screenshots/19-import-preview.png', fullPage: true });

  await page.getByRole('button', { name: 'Імпортувати' }).click();
  await page.getByRole('alertdialog').getByRole('button', { name: 'Імпортувати' }).click();
  await expect(page.getByRole('heading', { name: 'Результат імпорту' })).toBeVisible({ timeout: 30_000 });
  await expect(page.getByText('записано').first()).toBeVisible();
  await page.screenshot({ path: 'e2e/screenshots/20-import-result.png', fullPage: true });

  await page.getByRole('button', { name: 'До журналу' }).last().click();
  await expect(page.getByText('імпорт').first()).toBeVisible();

  await page.goto('/transactions/import');
  await page.getByRole('button', { name: 'Скасувати' }).first().click();
  await page.getByRole('alertdialog').getByRole('button', { name: 'Скасувати імпорт' }).click();
  await expect(page.getByText('скасовано').first()).toBeVisible();
});
