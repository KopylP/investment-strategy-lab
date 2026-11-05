import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  output,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { DraftStore } from '../../data-access/draft-store';
import { resultsUsing } from '../../data-access/shared-graph';
import { conditionRows } from '../../data-access/condition-rows-mapper';
import { graphWithNode } from '../../data-access/graph-mapper';
import { DraftProblemDto, NodeDto } from '../../models/draft-dto';
import { blocks } from '../../models/draft-vm';
import { ConditionRows } from './condition-rows';
import { ReadonlyCondition } from './readonly-condition';
import { Inspector } from '../inspector/inspector';
import { SetupDialog } from '../setup-dialog/setup-dialog';
import { ReportsDialog } from '../reports-dialog/reports-dialog';
import { ProblemsPanel } from '../problems-panel/problems-panel';

@Component({
  selector: 'app-builder-steps-view',
  imports: [
    FormsModule,
    Button,
    InputText,
    SetupDialog,
    ReportsDialog,
    ConditionRows,
    ReadonlyCondition,
    Inspector,
    ProblemsPanel,
  ],
  templateUrl: './steps-view.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StepsView {
  protected readonly store = inject(DraftStore);
  readonly chooseProblem = output<DraftProblemDto>();
  readonly editSetup = output<void>();
  protected readonly guidance = [
    'Дайте стратегії зрозумілу назву та опишіть інвестиційну ідею. Зміни зберігаються автоматично.',
    'Оберіть доступні для торгівлі фонди, бенчмарк для порівняння та фонд для вільних коштів.',
    'Визначте, за яких ринкових умов стратегія може інвестувати.',
    'Визначте, коли продавати фонд, який уже є в портфелі.',
    'Визначте, коли фонд може стати кандидатом на купівлю.',
    'Оберіть, як ранжувати кандидатів і розподіляти ваги портфеля.',
    'За потреби додайте умови позачергового виходу між ребалансуваннями.',
    'Оберіть звіти та частоту їх отримання.',
    'Перевірте правила та виправте помилки. Потім запустіть бектест або опублікуйте стратегію кнопками вгорі.',
  ];
  readonly editReports = output<void>();
  protected readonly step = signal(0);
  protected readonly titles = [
    'Назва й опис',
    'Фонди й ряди',
    ...blocks.map((block) => block.title),
    'Рапорти',
    'Перевірка й бектест',
  ];
  protected readonly block = computed(() => blocks[this.step() - 2]?.kind ?? null);
  protected readonly rows = computed(() => {
    const draft = this.store.draft();
    const block = this.block();
    return draft && block ? conditionRows(draft.graph, block) : null;
  });
  protected readonly result = computed(() =>
    this.store
      .draft()
      ?.graph.nodes.find((node) => node.block === this.block() && node.kind.endsWith('Result')),
  );
  protected readonly sentence = computed(
    () =>
      this.store.draft()?.words.find((words) => words.block === this.block())?.sentence ??
      'Не налаштовано',
  );

  constructor() {
    const selected = this.store.selectedNodeId();
    const node = this.store.draft()?.graph.nodes.find((node) => node.id === selected);
    const context =
      node && this.store.draft()
        ? resultsUsing(this.store.draft()!.graph, node.id)[0]?.block
        : node?.block;
    const index = blocks.findIndex((block) => block.kind === context);
    if (index >= 0) this.step.set(index + 2);
  }

  protected updateNode(node: NodeDto): void {
    const draft = this.store.draft();
    if (draft) this.store.changeGraph(graphWithNode(draft.graph, node));
    this.store.select(node.id);
  }

  protected async editGraph(): Promise<void> {
    const draft = this.store.draft();
    const block = this.block();
    if (draft && block) {
      this.store.moveNodes({
        ...draft.layout,
        collapsed: draft.layout.collapsed.filter((kind) => kind !== block),
      });
    }
    await this.store.setMode('graph');
  }
}
