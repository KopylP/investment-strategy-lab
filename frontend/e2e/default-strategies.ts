import { expect, Page } from '@playwright/test';

export async function defaultStrategies(page: Page): Promise<{ id: string; name: string }[]> {
  let strategies: { id: string; name: string }[] = [];

  await expect(async () => {
    await page.request.post('/api/strategies/defaults', { data: {} });
    strategies = await (await page.request.get('/api/strategies')).json();
    expect(strategies.length).toBeGreaterThan(1);
  }).toPass({ timeout: 20_000 });

  return strategies;
}
