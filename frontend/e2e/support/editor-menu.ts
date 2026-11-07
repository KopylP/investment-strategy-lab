import { type Page } from '@playwright/test';

export async function chooseFromEditorMenu(page: Page, menu: string, item: string): Promise<void> {
  await page.locator('.editor-menubar').getByRole('menuitem', { name: menu, exact: true }).click();
  await page.getByRole('menuitem', { name: item, exact: true }).click();
}

export async function openPublishConfirmation(page: Page): Promise<void> {
  await page.getByRole('button', { name: 'Інші дії зі збереженням', exact: true }).click();
  await page.getByRole('menuitem', { name: 'Опублікувати…', exact: true }).click();
}
