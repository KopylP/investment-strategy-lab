import { ClassicPreset, GetSchemes } from 'rete';
import { NodeDto } from '../../models/draft-dto';
import { SocketType, VocabularyDto } from '../../models/vocabulary-dto';

export class BuilderSocket extends ClassicPreset.Socket {
  constructor(readonly type: SocketType) {
    super(type);
  }
}

export class BuilderNode extends ClassicPreset.Node {
  override id: string;

  constructor(
    readonly draftNode: NodeDto,
    vocabulary: VocabularyDto,
  ) {
    super(draftNode.kind);
    this.id = draftNode.id;

    const kind = vocabulary.nodeKinds.find((candidate) => candidate.kind === draftNode.kind);
    for (const input of kind?.inputs ?? []) {
      this.addInput(
        input.name,
        new ClassicPreset.Input(new BuilderSocket(input.type), input.name, input.many),
      );
    }
    if (kind?.output)
      this.addOutput(
        'value',
        new ClassicPreset.Output(new BuilderSocket(kind.output), kind.output),
      );
  }
}

export class BuilderConnection extends ClassicPreset.Connection<BuilderNode, BuilderNode> {
  constructor(
    source: BuilderNode,
    target: BuilderNode,
    readonly inputName: string,
  ) {
    super(source, 'value', target, inputName);
  }
}

export type BuilderSchemes = GetSchemes<BuilderNode, BuilderConnection>;
