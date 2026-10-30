import { expect, type Locator, type Page, test } from '@playwright/test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';

/** A phone (iPhone 15 size, touch): no page, open dropdown, date picker, menu or expanded row stretches the page or a table sideways. */
test.use({ viewport: { width: 390, height: 844 }, hasTouch: true, isMobile: true, deviceScaleFactor: 3 });
// Thirteen pages, each dropdown opened in turn.
test.setTimeout(240_000);

/**
 * Neither the page nor anything in it (a table's container) scrolls sideways. html and body clip horizontal overflow,
 * which would hide a too-wide page from scrollWidth, so the clip is lifted while measuring.
 */
async function expectNoSidewaysScroll(page: Page, where: string): Promise<void> {
  const { scroll, width, scrollers } = await page.evaluate(() => {
    const unclip = document.createElement('style');
    unclip.textContent = 'html, body { overflow-x: visible !important; }';
    document.head.append(unclip);
    const measured = {
      scroll: document.documentElement.scrollWidth,
      width: window.innerWidth,
      scrollers: [...document.querySelectorAll<HTMLElement>('body *')]
        .filter((element) => ['auto', 'scroll'].includes(getComputedStyle(element).overflowX) && element.scrollWidth > element.clientWidth + 1)
        .map((element) => `${element.className} ${element.scrollWidth}px in ${element.clientWidth}px`),
    };
    unclip.remove();
    return measured;
  });
  expect(scroll, `${where}: the page is ${scroll}px wide on a ${width}px screen`).toBeLessThanOrEqual(width);
  expect(scrollers, `${where}: scrolls sideways inside`).toEqual([]);
}

/** Opens each expandable row of the page, all at once, checks the width, and closes them again. */
async function openEachRow(page: Page, where: string): Promise<void> {
  const togglers = page.locator('.layout-main button[aria-label="Лоти"]:visible, .layout-main button[aria-label="Деталі"]:visible');
  const count = await togglers.count();
  if (!count) return;
  for (let index = 0; index < count; index++) await togglers.nth(index).click();
  await expectNoSidewaysScroll(page, `${where}, ${count} rows open`);
  for (let index = 0; index < count; index++) await togglers.nth(index).click();
  await expectNoSidewaysScroll(page, `${where}, rows closed again`);
}

const overlays = '.p-select-overlay, .p-datepicker-panel, .p-tieredmenu-overlay';

/** Opens each visible dropdown, date picker and split-button menu of the page in turn and checks the width with it open. */
async function openEachOverlay(page: Page, where: string, within: Locator = page.locator('body')): Promise<void> {
  const triggers = within.locator('.p-select:visible, .p-datepicker-input:visible, .p-splitbutton-dropdown:visible');
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
    // A select with a filter in a dialog can lose the focus to the dialog, and with it the Escape: tap the field again.
    if (!(await page.locator(overlays).first().waitFor({ state: 'detached', timeout: 2_000 }).then(() => true, () => false))) await trigger.click();
    await page.locator(overlays).first().waitFor({ state: 'detached' });
  }
}

async function check(page: Page, path: string, name: string): Promise<void> {
  await page.goto(path);
  await page.locator('h1').first().waitFor();
  await page.locator('.p-skeleton').first().waitFor({ state: 'detached' });
  await expectNoSidewaysScroll(page, name);
  await openEachOverlay(page, name);
  await openEachRow(page, name);
  await page.screenshot({ path: `e2e/screenshots/phone-${name}.png`, fullPage: true });
}

test('every page fits a phone, with any dropdown or row open, and the tab bar reaches the sections', async ({ page }) => {
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
  // The app may have made them already on the first page after sign-up; then this call makes none and returns [].
  const made = await defaults.json();
  const strategies = made.length ? made : await (await api.get('/api/strategies')).json();
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
  // A fractional buy at a high price: the long quantities and amounts a real position has (16,81809024 шт. × 1 498,0856).
  const [instrument] = await (await api.get('/api/market-data/instruments')).json();
  const boughtOn = new Date();
  boughtOn.setMonth(boughtOn.getMonth() - 2);
  expect((await api.post(`/api/portfolio/accounts/${account.id}/contributions`, { data: { amount: 30_000, occurredOn: asOf.toISOString().slice(0, 10) } })).ok()).toBeTruthy();
  const bought = await api.post(`/api/portfolio/accounts/${account.id}/trades`, {
    data: {
      instrumentId: instrument.id, side: 'Buy', quantity: 16.81809024, price: 1498.0856, fee: 1.23, executedOn: boughtOn.toISOString().slice(0, 10),
      allocations: [{ subPortfolioId: account.subPortfolios[1].id, quantity: 16.81809024 }],
    },
  });
  expect(bought.ok(), `trade: ${await bought.text()}`).toBeTruthy();

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

  // Positions in PLN: every amount carries its original currency, the longest lines the page has.
  await page.goto('/holdings');
  await page.locator('.layout-topbar').getByText('PLN', { exact: true }).click();
  await page.locator('.p-skeleton').first().waitFor({ state: 'detached' });
  await expectNoSidewaysScroll(page, 'holdings in PLN');
  await openEachRow(page, 'holdings in PLN');

  // The ledger's dialogs: their own dropdowns and date pickers.
  for (const button of ['Угода', 'Кошти']) {
    await page.goto('/transactions');
    // The name starts with the icon font's glyph.
    await page.getByRole('button', { name: new RegExp(`${button}$`) }).click();
    await expect(page.getByRole('dialog')).toBeVisible();
    await openEachOverlay(page, `ledger, dialog «${button}»`, page.getByRole('dialog'));
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
