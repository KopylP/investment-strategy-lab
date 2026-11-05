import { Page } from '@playwright/test';

export async function mockStrategyWorkspace(page: Page) {
  const blocks = ['MarketFilter', 'Exit', 'Entry', 'Selection', 'Risk'];
  const kinds = ['MarketResult', 'ExitResult', 'EntryResult', 'SelectionResult', 'RiskResult'];
  const nodes = blocks.flatMap((block, i) => [
    { id: `condition-${i}`, kind: 'All', block, parameters: {} },
    { id: `result-${i}`, kind: kinds[i], block, parameters: {} },
  ]);
  nodes.push(
    {
      id: 'number',
      kind: 'Indicator',
      block: 'MarketFilter',
      parameters: { indicator: 'momentum', period: 20, of: { series: 'fund' } },
    },
    { id: 'compare', kind: 'Compare', block: 'MarketFilter', parameters: {} },
    { id: 'signal', kind: 'Signal', block: 'MarketFilter', parameters: {} },
  );
  let draft: any = {
    id: 'workspace',
    name: 'Перевірка UX',
    description: '',
    revision: 1,
    updatedAt: new Date().toISOString(),
    originStrategyId: null,
    originVersion: null,
    setup: { funds: [], benchmarkId: null, cashId: null, volatilityIndexId: null, extraSeries: [] },
    graph: {
      nodes,
      links: blocks.map((_, i) => ({
        from: `condition-${i}`,
        to: `result-${i}`,
        input: 'condition',
      })),
    },
    layout: {
      positions: Object.fromEntries(
        nodes.map((n, i) => [
          n.id,
          { x: i < 10 ? (i % 2 ? 500 : 32) : 32 + (i - 10) * 260, y: i < 10 ? 32 : 210 },
        ]),
      ),
      collapsed: [],
    },
    reports: [],
    problems: [],
    words: [],
  };
  const vocabulary = {
    indicators: [
      {
        code: 'momentum',
        label: 'Моментум',
        fields: [
          {
            name: 'period',
            label: 'Період',
            kind: 'Integer',
            minimum: 1,
            maximum: 365,
            default: 20,
            required: true,
            choiceLabels: {},
          },
        ],
      },
    ],
    conditions: [
      { code: 'all', label: 'Усі умови', fields: [] },
      {
        code: 'compare',
        label: 'Порівняння',
        fields: [
          {
            name: 'value',
            label: 'Поріг',
            kind: 'Number',
            minimum: null,
            maximum: null,
            default: 0,
            required: true,
            choiceLabels: {},
          },
        ],
      },
    ],
    blockFields: {},
    reports: [],
    templates: [],
    nodeKinds: ['All', ...kinds].map((kind) => ({
      kind,
      output: kind === 'All' ? 'Boolean' : null,
      inputs: [{ name: 'condition', type: 'Boolean', many: kind === 'All', required: true }],
    })),
  };
  vocabulary.nodeKinds.push(
    { kind: 'Indicator', output: 'Number', inputs: [] },
    {
      kind: 'Compare',
      output: 'Boolean',
      inputs: [{ name: 'left', type: 'Number', many: false, required: true }],
    },
    { kind: 'Signal', output: 'Boolean', inputs: [] },
  );
  await page.route('**/api/**', async (route) => {
    const path = new URL(route.request().url()).pathname;
    let body: any = [];
    if (path === '/api/identity/me') body = { id: 'test', email: 'test@example.com' };
    if (path === '/api/identity/profile')
      body = { displayCurrency: 'USD', locale: 'uk-UA', timeZone: 'Europe/Warsaw' };
    if (path.endsWith('/vocabulary')) body = vocabulary;
    if (path.endsWith('/drafts/workspace')) {
      if (route.request().method() === 'PUT') {
        draft = { ...draft, ...route.request().postDataJSON(), revision: draft.revision + 1 };
      }
      body = draft;
    }
    if (path.endsWith('/layout')) draft.layout = route.request().postDataJSON();
    if (path.endsWith('/check'))
      body = {
        isClean: false,
        problems: [
          {
            message: 'Оберіть фонди',
            severity: 'Error',
            nodeId: null,
            block: null,
            reportCode: null,
          },
        ],
      };
    await route.fulfill({ json: body });
  });
  return {
    get draft() {
      return draft;
    },
  };
}
