import { Injector } from '@angular/core';
import { AreaPlugin } from 'rete-area-plugin';
import { BuilderSchemes } from './graph-schemes';

export type BuilderAreaExtra = never;

export class GraphRenderer {
  attach(area: AreaPlugin<BuilderSchemes, BuilderAreaExtra>, injector: Injector): void {
    void area;
    void injector;
  }
}
