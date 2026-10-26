import { ChangeDetectionStrategy, Component, computed, inject, input, model } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Fieldset } from 'primeng/fieldset';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { Tooltip } from 'primeng/tooltip';
import { formatQuantity } from '../../pipes/format';
import { Viewport } from '../viewport';

type JsonValue = string | number | boolean | null | JsonObject | JsonValue[];
export interface JsonObject {
  [key: string]: JsonValue;
}

interface SchemaNode {
  type?: string;
  title?: string;
  description?: string;
  properties?: Record<string, SchemaNode>;
  $ref?: string;
  allOf?: SchemaNode[];
  enum?: (string | number)[];
  /** Human labels of enum values, published by the engine next to the enum. */
  'x-enumLabels'?: Record<string, string>;
  minimum?: number;
  maximum?: number;
  exclusiveMinimum?: number;
  exclusiveMaximum?: number;
  default?: JsonValue;
}

interface FieldVm {
  path: string[];
  key: string;
  label: string;
  description?: string;
  kind: 'number' | 'integer' | 'boolean' | 'enum' | 'text';
  min?: number;
  max?: number;
  options?: { label: string; value: string | number }[];
}

interface GroupVm {
  title: string;
  fields: FieldVm[];
}

/** Fields for strategy parameters, built from the JSON Schema the engine publishes (§11.5). */
@Component({
  selector: 'app-schema-form',
  imports: [FormsModule, Fieldset, InputNumber, InputText, Select, ToggleSwitch, Tooltip],
  templateUrl: './schema-form.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SchemaForm {
  readonly schema = input.required<SchemaNode>();
  readonly value = model.required<JsonObject>();
  readonly disabled = input(false);

  protected readonly viewport = inject(Viewport);

  protected readonly groups = computed(() => toGroups(this.schema()));

  protected read(path: string[]): JsonValue {
    let node: JsonValue = this.value();
    for (const key of path) node = node && typeof node === 'object' && !Array.isArray(node) ? (node[key] ?? null) : null;
    return node;
  }

  /** A read-only value as text: "так" / "ні", an enum's label, a number in the user's locale. */
  protected shown(field: FieldVm): string {
    const value = this.read(field.path);
    if (value === null) return '—';
    if (field.kind === 'boolean') return value ? 'так' : 'ні';
    if (field.kind === 'enum') return field.options?.find((option) => option.value === value)?.label ?? String(value);
    return typeof value === 'number' ? formatQuantity(value) : String(value);
  }

  protected write(path: string[], next: JsonValue): void {
    this.value.update((current) => setAt(current, path, next));
  }
}

function toGroups(root: SchemaNode): GroupVm[] {
  const defs = (root as { $defs?: Record<string, SchemaNode> }).$defs ?? {};
  const resolve = (node: SchemaNode): SchemaNode => {
    const ref = node.$ref ?? node.allOf?.find((part) => part.$ref)?.$ref;
    return ref ? { ...defs[ref.split('/').pop()!], title: node.title ?? defs[ref.split('/').pop()!]?.title } : node;
  };

  const general: GroupVm = { title: 'Загальні', fields: [] };
  const groups: GroupVm[] = [general];
  const walk = (node: SchemaNode, path: string[], group: GroupVm) => {
    for (const [key, raw] of Object.entries(resolve(node).properties ?? {})) {
      const property = resolve(raw);
      if (property.type === 'object' || property.properties) {
        const nested: GroupVm = { title: humanize(raw.title ?? key), fields: [] };
        groups.push(nested);
        walk(property, [...path, key], nested);
      } else {
        group.fields.push(toField(property, [...path, key], key));
      }
    }
  };
  walk(root, [], general);
  return groups.filter((group) => group.fields.length);
}

function toField(property: SchemaNode, path: string[], key: string): FieldVm {
  const kind: FieldVm['kind'] = property.enum ? 'enum' : property.type === 'integer' ? 'integer' : property.type === 'number' ? 'number' : property.type === 'boolean' ? 'boolean' : 'text';
  return {
    path,
    key,
    label: humanize(property.title ?? key),
    description: property.description,
    kind,
    min: property.minimum ?? property.exclusiveMinimum,
    max: property.maximum ?? property.exclusiveMaximum,
    options: property.enum?.map((value) => ({ label: property['x-enumLabels']?.[String(value)] ?? String(value), value })),
  };
}

/** "topN" / "Top N" → "Top n". */
function humanize(text: string): string {
  const spaced = text.replace(/([a-z0-9])([A-Z])/g, '$1 $2').replace(/_/g, ' ').trim();
  return spaced.charAt(0).toUpperCase() + spaced.slice(1).toLowerCase();
}

function setAt(target: JsonObject, path: string[], value: JsonValue): JsonObject {
  const [head, ...rest] = path;
  const child = target[head];
  return {
    ...target,
    [head]: rest.length ? setAt(child && typeof child === 'object' && !Array.isArray(child) ? child : {}, rest, value) : value,
  };
}
