import { ClassicPreset, GetSchemes } from 'rete';
import { NodeDto, DraftProblemDto } from '../../models/draft-dto';
import { VocabularyDto } from '../../models/vocabulary-dto';
import { OwnSignalDto } from '../../models/own-signal-dto';
import { InstrumentChoiceDto } from '../../models/instrument-dto';
import { GraphDto } from '../../models/draft-dto';

export class StrategyNode extends ClassicPreset.Node {
  width = 240;
  height = 140;
  overlayContainer: HTMLElement | 'body' = 'body';
  override selected = false;
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
    public outputClick: () => void,
  ) {
    super(dto.kind);
    this.id = dto.id;
    this.width =
      dto.kind === 'Indicator' ? 256 : ['All', 'Any', 'Not'].includes(dto.kind) ? 224 : 240;
    const kind = vocabulary.nodeKinds.find((k) => k.kind === dto.kind);
    for (const input of kind?.inputs ?? [])
      this.addInput(
        input.name,
        new ClassicPreset.Input(new ClassicPreset.Socket(input.type), input.name, input.many),
      );
    if (kind?.output)
      this.addOutput(
        'output',
        new ClassicPreset.Output(new ClassicPreset.Socket(kind.output), kind.output),
      );
  }
}
export type StrategyConnection = ClassicPreset.Connection<ClassicPreset.Node, ClassicPreset.Node>;
export type Schemes = GetSchemes<StrategyNode, StrategyConnection>;
