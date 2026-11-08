import { translate } from '@jsverse/transloco';
import { ClassicPreset, GetSchemes } from 'rete';
import { NodeDto, DraftProblemDto, DraftSetupDto } from '../../models/draft-dto';
import { VocabularyDto } from '../../models/vocabulary-dto';
import { OwnSignalDto } from '../../models/own-signal-dto';
import { InstrumentChoiceDto } from '../../models/instrument-dto';
import { setupFields } from '../../data-access/setup-bindings';
import { EdgeHandle, Point } from './edge-routing';
import { GraphDto } from '../../models/draft-dto';

export class StrategyNode extends ClassicPreset.Node {
  width = 240;
  height = 140;
  overlayContainer: HTMLElement | 'body' = 'body';
  override selected = false;
  branchDimmed = false;
  problem?: DraftProblemDto;
  compatible: string[] = [];
  connecting = false;
  constructor(
    public dto: NodeDto,
    public vocabulary: VocabularyDto,
    public graph: GraphDto,
    public signals: OwnSignalDto[],
    public instruments: InstrumentChoiceDto[],
    public change: (node: NodeDto) => void,
    public choose: () => void,
    public duplicate: () => void,
    public remove: () => void,
    public inputClick: (key: string) => void,
    public outputClick: (key: string) => void,
    public setup: DraftSetupDto,
    public changeSetup: (setup: DraftSetupDto) => void,
    public refreshInstruments: () => void,
  ) {
    super(dto.kind);
    this.id = dto.id;
    this.width =
      dto.kind === 'Indicator' ? 256 : ['All', 'Any', 'Not'].includes(dto.kind) ? 224 : 240;
    if (dto.kind === 'Setup') this.width = 320;
    if (dto.kind === 'FundSelection' || dto.kind === 'SelectionResult') this.width = 264;
    if (dto.kind === 'Cash') this.width = 224;
    if (dto.kind === 'Portfolio') this.width = 280;
    const kind = vocabulary.nodeKinds.find((k) => k.kind === dto.kind);
    for (const input of kind?.inputs ?? [])
      this.addInput(
        input.name,
        new ClassicPreset.Input(new ClassicPreset.Socket(input.type), input.name, input.many),
      );
    if (dto.kind === 'Indicator' && !this.inputs['of'])
      this.addInput(
        'of',
        new ClassicPreset.Input(
          new ClassicPreset.Socket('Series'),
          translate('builder.graph.socket.series'),
          false,
        ),
      );
    if (dto.kind === 'Portfolio')
      for (const [name, type] of [
        ['funds', 'Funds'],
        ['cash', 'Budget'],
        ['risk', 'RiskPolicy'],
      ])
        if (!this.inputs[name])
          this.addInput(name, new ClassicPreset.Input(new ClassicPreset.Socket(type), name));
    if (dto.kind === 'Cash')
      this.addOutput(
        'output',
        new ClassicPreset.Output(
          new ClassicPreset.Socket('Budget'),
          translate('builder.graph.socket.inCash'),
        ),
      );
    if (dto.kind === 'RiskResult' && !kind?.output)
      this.addOutput(
        'output',
        new ClassicPreset.Output(
          new ClassicPreset.Socket('RiskPolicy'),
          translate('builder.graph.socket.control'),
        ),
      );
    if (dto.kind === 'Setup') this.updateSetupOutputs();
    if (dto.kind === 'Cash')
      this.addOutput(
        'series',
        new ClassicPreset.Output(
          new ClassicPreset.Socket('Series'),
          translate('builder.graph.socket.cashSeries'),
        ),
      );
    if (dto.kind === 'SelectionResult')
      this.addOutput(
        'cash',
        new ClassicPreset.Output(
          new ClassicPreset.Socket('Budget'),
          translate('builder.graph.socket.remainder'),
        ),
      );
    if (kind?.output)
      this.addOutput(
        'output',
        new ClassicPreset.Output(new ClassicPreset.Socket(kind.output), kind.output),
      );
  }
  updateSetupOutputs() {
    if (this.dto.kind !== 'Setup') return;
    const keys = [
      ...setupFields
        .filter((field) => field.key !== 'cashId')
        .map((field) => ({ key: field.key, type: field.type as string })),
      ...new Set([
        ...this.setup.extraSeries.map((id) => `extra:${id}`),
        ...this.graph.links
          .filter((link) => link.from === this.id && link.output?.startsWith('extra:'))
          .map((link) => link.output!),
      ]).values(),
    ].map((value) => (typeof value === 'string' ? { key: value, type: 'Series' } : value));
    for (const field of keys)
      if (!this.outputs[field.key])
        this.addOutput(
          field.key,
          new ClassicPreset.Output(new ClassicPreset.Socket(field.type), field.key),
        );
    for (const key of Object.keys(this.outputs))
      if (!keys.some((field) => field.key === key)) this.removeOutput(key);
  }
}
export type StrategyConnection = ClassicPreset.Connection<
  ClassicPreset.Node,
  ClassicPreset.Node
> & {
  selected?: boolean;
  branchDimmed?: boolean;
  handles?: EdgeHandle[];
  handleRadius?: number;
  choose?: () => void;
  remove?: () => void;
  dragStart?: (segment: number) => void;
  dragMove?: (point: Point) => void;
  dragEnd?: (cancel: boolean) => void;
};
export type Schemes = GetSchemes<StrategyNode, StrategyConnection>;
