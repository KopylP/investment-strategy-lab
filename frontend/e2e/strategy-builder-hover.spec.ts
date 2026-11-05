import { expect, Locator, test } from '@playwright/test';
import { mockStrategyWorkspace } from './support/strategy-workspace';

async function contrast(locator: Locator) {
  return locator.evaluate((el) => {
    const canvas = document.createElement('canvas');
    canvas.width = canvas.height = 1;
    const context = canvas.getContext('2d')!;
    const rgba = (color: string) => {
      context.clearRect(0, 0, 1, 1);
      context.fillStyle = color;
      context.fillRect(0, 0, 1, 1);
      const c = context.getImageData(0, 0, 1, 1).data;
      return [c[0], c[1], c[2], c[3] / 255];
    };
    const composite = (front: number[], back: number[]) =>
      front
        .slice(0, 3)
        .map((c, i) => c * front[3] + back[i] * (1 - front[3]))
        .concat(1);
    let background = [255, 255, 255, 1];
    const ancestors: Element[] = [];
    for (let current: Element | null = el; current; current = current.parentElement)
      ancestors.unshift(current);
    let opacity = 1;
    for (const ancestor of ancestors) {
      const style = getComputedStyle(ancestor);
      background = composite(rgba(style.backgroundColor), background);
      opacity *= Number(style.opacity);
    }
    const color = rgba(getComputedStyle(el).color);
    color[3] *= opacity;
    const foreground = composite(color, background);
    const luminance = (rgb: number[]) =>
      rgb.slice(0, 3).reduce((sum, c, i) => {
        const v = c / 255;
        return (
          sum +
          [0.2126, 0.7152, 0.0722][i] * (v <= 0.04045 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4)
        );
      }, 0);
    const a = luminance(foreground),
      b = luminance(background);
    return { ratio: (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05), foreground, background };
  });
}

for (const dark of [false, true]) {
  test(`editor hover and focus remain readable in ${dark ? 'dark' : 'light'} theme`, async ({
    page,
  }, testInfo) => {
    // The app theme must also work when it differs from the OS setting.
    await page.emulateMedia({ colorScheme: dark ? 'light' : 'dark' });
    await page.addInitScript(
      (value) => localStorage.setItem('investmentlab.dark', String(value)),
      dark,
    );
    await mockStrategyWorkspace(page);
    await page.goto('/strategy-builder/workspace');
    await expect
      .poll(() => page.locator('html').evaluate((el) => el.classList.contains('app-dark')))
      .toBe(dark);
    await expect(page.locator('app-node-palette .item').first()).toBeVisible();
    const check = async (locator: Locator, minimum: number) => {
      await expect
        .poll(async () => (await contrast(locator)).ratio)
        .toBeGreaterThanOrEqual(minimum);
    };
    const item = page.locator('app-node-palette .item').first();
    await item.hover();
    await check(item, 4.5);
    await check(item.locator('small'), 4.5);
    await check(item.locator('.plus'), 3);
    await page.screenshot({ path: testInfo.outputPath('palette-hover.png') });
    const controls = [
      page.getByRole('button', { name: 'Показати всю схему', exact: true }),
      page.getByRole('button', { name: 'На весь екран', exact: true }),
      page
        .getByRole('navigation', { name: 'Ролі стратегії' })
        .getByRole('button', { name: 'Вхід', exact: true }),
      page.getByRole('button', { name: /Налаштування/ }),
    ];
    for (const control of controls) {
      await control.hover();
      await check(control, 4.5);
    }
    await controls[0].hover();
    await page.screenshot({ path: testInfo.outputPath('toolbar-hover.png') });
    await controls[0].focus();
    await page.mouse.move(0, 0);
    await check(controls[0], 4.5);
    await controls[2].click();
    const duplicate = page
      .locator('[data-node-id="condition-2"]')
      .getByRole('button', { name: 'Дублювати', exact: true });
    await duplicate.hover();
    await check(duplicate.locator('i'), 3);
    await page.screenshot({ path: testInfo.outputPath('node-hover.png') });
    const canvas = page.locator('[data-block-canvas]');
    const background = await canvas.evaluate((el) => getComputedStyle(el).backgroundColor);
    const expected = await canvas.evaluate((el) => {
      const probe = document.createElement('div');
      probe.style.background = 'var(--surface-ground)';
      el.append(probe);
      const value = getComputedStyle(probe).backgroundColor;
      probe.remove();
      return value;
    });
    expect(background).toBe(expected);
    // Changing the theme without reloading updates an already hovered control.
    await page
      .getByRole('button', { name: dark ? 'Світла тема' : 'Темна тема', exact: true })
      .click();
    await expect
      .poll(() => page.locator('html').evaluate((el) => el.classList.contains('app-dark')))
      .toBe(!dark);
    await item.hover();
    await check(item, 4.5);
    await check(item.locator('small'), 4.5);
  });
}
