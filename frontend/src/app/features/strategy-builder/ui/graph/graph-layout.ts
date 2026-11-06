import { ElkNode } from 'elkjs/lib/elk-api';
import { GraphDto } from '../../models/draft-dto';
import { Point } from './edge-routing';

export interface LayoutNode {
  id: string;
  width: number;
  height: number;
  ports: { id: string; side: 'WEST' | 'EAST'; y: number }[];
}
export const portId = (node: string, side: 'input' | 'output', key: string) =>
  `${node}:${side}:${key}`;
export const edgeId = (from: string, output: string, to: string, input: string) =>
  [from, output, to, input].map((value) => `${value.length}:${value}`).join('|');

export async function layoutGraph(graph: GraphDto, nodes: LayoutNode[]) {
  const { default: ELK } = await import('elkjs/lib/elk.bundled.js');
  const elk = new ELK();
  const input: ElkNode = {
    id: 'strategy',
    layoutOptions: {
      'elk.algorithm': 'layered',
      'elk.direction': 'RIGHT',
      'elk.edgeRouting': 'ORTHOGONAL',
      'elk.padding': '[top=48,left=48,bottom=48,right=48]',
      'elk.spacing.nodeNode': '80',
      'elk.spacing.edgeNode': '28',
      'elk.spacing.edgeEdge': '18',
      'elk.layered.spacing.nodeNodeBetweenLayers': '120',
      'elk.layered.spacing.edgeNodeBetweenLayers': '32',
      'elk.layered.spacing.edgeEdgeBetweenLayers': '18',
      'elk.layered.nodePlacement.strategy': 'BRANDES_KOEPF',
      'elk.layered.considerModelOrder.strategy': 'NODES_AND_EDGES',
      'elk.separateConnectedComponents': 'false',
    },
    children: nodes.map((node) => ({
      id: node.id,
      width: node.width,
      height: node.height,
      layoutOptions: { 'elk.portConstraints': 'FIXED_POS' },
      ports: node.ports.map((port) => ({
        id: port.id,
        width: 0,
        height: 0,
        x: port.side === 'WEST' ? 0 : node.width,
        y: port.y,
        layoutOptions: { 'elk.port.side': port.side },
      })),
    })),
    edges: graph.links
      // A cash price is a reference series, independent of its later allocation.
      .filter((link) => !(link.from === 'strategy-cash' && link.output === 'series'))
      .filter(
        (link) =>
          nodes.some((node) =>
            node.ports.some(
              (port) => port.id === portId(link.from, 'output', link.output ?? 'output'),
            ),
          ) &&
          nodes.some((node) =>
            node.ports.some((port) => port.id === portId(link.to, 'input', link.input)),
          ),
      )
      .map((link) => ({
        id: edgeId(link.from, link.output ?? 'output', link.to, link.input),
        sources: [portId(link.from, 'output', link.output ?? 'output')],
        targets: [portId(link.to, 'input', link.input)],
        layoutOptions: {
          'elk.layered.priority.straightness': link.input === 'funds' ? '100' : '0',
        },
      })),
  };
  const result = await elk.layout(input);
  const positions = Object.fromEntries(
    (result.children ?? []).map((node) => [node.id, { x: node.x ?? 0, y: node.y ?? 0 }]),
  );
  const routes = new Map<string, Point[]>();
  for (const edge of result.edges ?? []) {
    const section = edge.sections?.[0];
    if (section)
      routes.set(edge.id, [section.startPoint, ...(section.bendPoints ?? []), section.endPoint]);
  }
  return { positions, routes };
}
