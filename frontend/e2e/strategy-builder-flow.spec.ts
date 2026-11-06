import { expect, test } from '@playwright/test';
import { mockStrategyWorkspace } from './support/strategy-workspace';

for (const template of ['momentum', 'dual', 'hold']) {
  test(`${template}: flow ends in a portfolio, reads left to right and routes around cards`, async ({
    page,
  }, testInfo) => {
    const errors: string[] = [];
    page.on('pageerror', (error) => errors.push(error.message));
    const workspace = await mockStrategyWorkspace(page, (draft, vocabulary) => {
      const nodes: any[] = [
        {
          id: 'result-market',
          kind: 'MarketResult',
          block: 'MarketFilter',
          parameters: { investWhenUnfavorable: 0.3 },
        },
        { id: 'result-exit', kind: 'ExitResult', block: 'Exit', parameters: {} },
        { id: 'result-entry', kind: 'EntryResult', block: 'Entry', parameters: {} },
        {
          id: 'result-selection',
          kind: 'SelectionResult',
          block: 'Selection',
          parameters: { top: template === 'hold' ? 60 : 3, order: 'desc', method: 'equal' },
        },
        { id: 'result-risk', kind: 'RiskResult', block: 'Risk', parameters: {} },
      ];
      const links: any[] = [];
      const indicator = (id: string, series: string) =>
        nodes.push({
          id,
          kind: 'Indicator',
          block: 'Shared',
          parameters: { indicator: 'momentum', period: 200, of: { series } },
        });
      const compare = (id: string, from: string) => {
        nodes.push({ id, kind: 'Compare', block: 'Shared', parameters: { value: 0 } });
        links.push({ from, to: id, input: 'left' });
      };
      if (template !== 'hold') {
        for (const role of ['entry', 'exit']) {
          for (let i = 0; i < 2; i++) {
            indicator(`${role}-indicator-${i}`, 'fund');
            compare(`${role}-compare-${i}`, `${role}-indicator-${i}`);
          }
          nodes.push({ id: `${role}-all`, kind: 'All', block: 'Shared', parameters: {} });
          for (let i = 0; i < 2; i++)
            links.push({ from: `${role}-compare-${i}`, to: `${role}-all`, input: 'conditions' });
          links.push({ from: `${role}-all`, to: `result-${role}`, input: 'condition' });
        }
        indicator('ranking', 'fund');
        links.push({ from: 'ranking', to: 'result-selection', input: 'rankBy' });
      }
      if (template === 'momentum') {
        indicator('market-price', 'benchmark');
        indicator('market-average', 'benchmark');
        compare('market-compare', 'market-price');
        links.push(
          { from: 'market-average', to: 'market-compare', input: 'right' },
          { from: 'market-compare', to: 'result-market', input: 'condition' },
        );
      }
      vocabulary.nodeKinds
        .find((kind: any) => kind.kind === 'Compare')
        .inputs.push({ name: 'right', type: 'Number', required: false, many: false });
      vocabulary.nodeKinds.find((kind: any) => kind.kind === 'All').inputs[0].name = 'conditions';
      draft.graph = { nodes, links };
      draft.layout = { positions: {}, collapsed: [] };
      draft.setup.funds = ['fund-a', 'fund-b', 'fund-c'];
    });
    await page.goto('/strategy-builder/workspace');
    await expect(page.locator('[data-portfolio-summary]')).toHaveCount(1);
    await expect
      .poll(() => Object.keys(workspace.draft.layout.positions).length)
      .toBeGreaterThan(5);
    await page.getByRole('button', { name: 'Упорядкувати', exact: true }).click();
    const issues = () =>
      page.evaluate(() => {
        const articles = [...document.querySelectorAll<HTMLElement>('app-graph-editor article')];
        const boxes = new Map(
          articles.map((el) => [el.dataset['nodeId'], el.getBoundingClientRect()]),
        );
        const problems: string[] = [];
        for (const path of document.querySelectorAll<SVGPathElement>('path[data-edge-from]')) {
          const from = path.dataset['edgeFrom']!,
            to = path.dataset['edgeTo']!;
          const a = boxes.get(from)!,
            b = boxes.get(to)!;
          if (a.right > b.left + 1) problems.push(`backwards ${from} -> ${to}`);
          const matrix = path.getScreenCTM()!;
          const length = path.getTotalLength();
          for (let d = 0; d <= length; d += 3) {
            const p = path.getPointAtLength(d).matrixTransform(matrix);
            for (const [id, box] of boxes) {
              if (id === from || id === to) continue;
              if (
                p.x > box.left + 1 &&
                p.x < box.right - 1 &&
                p.y > box.top + 1 &&
                p.y < box.bottom - 1
              ) {
                problems.push(`${from} -> ${to} through ${id}`);
                break;
              }
            }
          }
          const keys = [
            path.dataset['edgeOutput'] && path.dataset['edgeOutput'] !== 'output'
              ? `${from}:output:${path.dataset['edgeOutput']}`
              : `${from}:output`,
            `${to}:input:${path.dataset['edgeInput']}`,
          ];
          keys.forEach((key, index) => {
            const socket = [...document.querySelectorAll<HTMLElement>('[data-socket-key]')].find(
              (el) => el.dataset['socketKey'] === key,
            );
            if (!socket) {
              problems.push(`missing ${key}`);
              return;
            }
            const r = socket.getBoundingClientRect(),
              p = path.getPointAtLength(index ? length : 0).matrixTransform(matrix);
            if (Math.hypot(p.x - r.left - r.width / 2, p.y - r.top - r.height / 2) > 1)
              problems.push(`detached ${key}`);
          });
        }
        return [...new Set(problems)];
      });
    await expect.poll(issues).toEqual([]);
    await page.screenshot({ path: testInfo.outputPath(`${template}-flow.png`) });
    await page.reload();
    await expect(page.locator('[data-portfolio-summary]')).toHaveCount(1);
    await expect.poll(issues).toEqual([]);
    expect(errors).toEqual([]);
  });
}
