import { expect, type Page, test } from '@playwright/test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';

/** A phone (iPhone 15 size, touch): no page, open dropdown, date picker or menu stretches the page sideways. */
test.use({ viewport: { width: 390, height: 844 }, hasTouch: true, isMobile: true, deviceScaleFactor: 3 });
// Thirteen pages, each dropdown opened in turn.
test.setTimeout(240_000);

async function expectNoSidewaysScroll(page: Page, where: string): Promise<void> {
  const { scroll, width } = await page.evaluate(() => ({ scroll: document.documentElement.scrollWidth, width: window.innerWidth }));
  expect(scroll, `${where}: the page is ${scroll}px wide on a ${width}px screen`).toBeLessThanOrEqual(width);
}

const overlays = '.p-select-overlay, .p-datepicker-panel, .p-tieredmenu-overlay';

/** Opens each visible dropdown, date picker and split-button menu of the page in turn and checks the width with it open. */
async function openEachOverlay(page: Page, where: string): Promise<void> {
  const triggers = page.locator('.p-select:visible, .p-datepicker-input:visible, .p-splitbutton-dropdown:visible');
  const count = await triggers.count();
  for (let index = 0; index < count; index++) {
    const trigger = triggers.nth(index);
    if (await trigger.evaluate((element) => element.closest('.p-disabled, [disabled]') !== null)) continue;
    await trigger.click();
    // An editable select (the time zone) takes the caret on a click instead of opening.
    const opened = await page.locator(overlays).first().waitFor({ timeout: 2_000 }).then(() => true, () => false);
    if (!opened) continue;
    await expectNoSidewaysScroll(page, `${where}, overlay ${index + 1} of ${count} open`);
    await page.keyboard.press('Escape');
    await page.locator(overlays).first().waitFor({ state: 'detached' });
  }
}

async function check(page: Page, path: string, name: string): Promise<void> {
  await page.goto(path);
  await page.locator('h1').first().waitFor();
  await page.locator('.p-skeleton').first().waitFor({ state: 'detached' });
  await expectNoSidewaysScroll(page, name);
  await openEachOverlay(page, name);
  await page.screenshot({ path: `e2e/screenshots/phone-${name}.png`, fullPage: true });
}

test('every page fits a phone, with any dropdown open, and the tab bar reaches the sections', async ({ page }) => {
  const email = `e2e-phone-${Date.now()}@example.com`;
  await page.goto('/auth/register');
  await expectNoSidewaysScroll(page, 'register');
  await page.locator('#email').fill(email);
  await page.locator('#password').fill('correct horse battery');
  await page.locator('#confirm').fill('correct horse battery');
  await page.getByRole('button', { name: 'Зареєструватися' }).click();
  await expect(page.getByRole('heading', { name: 'Огляд' })).toBeVisible();

  const api = page.request;
  const defaults = await api.post('/api/strategies/defaults', { data: {} });
  expect(defaults.ok(), `strategies/defaults: ${defaults.status()} ${await defaults.text()}`).toBeTruthy();
  const strategies = await defaults.json();
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

  const pages: [string, string][] = [
    ['/', 'overview'],
    ['/reports', 'reports'],
    ['/holdings', 'holdings'],
    ['/transactions', 'ledger'],
    ['/transactions/import', 'import'],
    ['/taxes', 'taxes'],
    ['/accounts', 'accounts'],
    ['/accounts/new', 'account-new'],
    [`/accounts/${account.id}`, 'account-detail'],
    ['/strategies', 'strategies'],
    [`/strategies/${strategies[0].id}`, 'strategy-detail'],
    ['/instruments', 'instruments'],
    ['/profile', 'profile'],
  ];
  for (const [path, name] of pages) await check(page, path, name);

  // The ledger's dialogs: their own dropdowns and date pickers.
  for (const button of ['Угода', 'Кошти']) {
    await page.goto('/transactions');
    // The name starts with the icon font's glyph.
    await page.getByRole('button', { name: new RegExp(`${button}$`) }).click();
    await expect(page.getByRole('dialog')).toBeVisible();
    await openEachOverlay(page, `ledger, dialog «${button}»`);
  }

  // The topbar's profile menu.
  await page.goto('/');
  await page.locator('.layout-topbar-action').filter({ has: page.locator('.pi-user') }).click();
  await expectNoSidewaysScroll(page, 'profile menu open');
  await page.keyboard.press('Escape');

  // The tab bar: five sections, the open one marked.
  const tabbar = page.getByRole('navigation', { name: 'Розділи' });
  await expect(tabbar).toBeVisible();
  await tabbar.getByRole('link', { name: 'Журнал' }).click();
  await expect(page).toHaveURL(/\/transactions$/);
  await expect(tabbar.getByRole('link', { name: 'Журнал' })).toHaveAttribute('aria-current', 'page');
  await expect(tabbar.getByRole('link', { name: 'Огляд' })).not.toHaveAttribute('aria-current', 'page');
});
