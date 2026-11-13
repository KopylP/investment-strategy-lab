import { DeclarationField } from '../models/declaration-dto';

export function formValue(field: DeclarationField): string {
  if (field.amount === null) return field.text ?? '';

  return Number.isInteger(field.amount)
    ? String(field.amount)
    : field.amount.toFixed(2).replace('.', ',');
}
