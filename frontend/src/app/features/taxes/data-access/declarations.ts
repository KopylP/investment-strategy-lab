export interface DeclarationField {
  position: number;
  label: string;
  amount: number | null;
  text: string | null;
  hint: string | null;
  computed: boolean;
}

export interface DeclarationSection {
  code: string;
  title: string;
  fields: DeclarationField[];
}

export interface TaxDeclaration {
  form: string;
  title: string;
  year: number;
  deadline: string;
  sections: DeclarationSection[];
  attachments: { form: string; title: string; fields: DeclarationField[] }[];
  accounts: DeclarationAccountLine[];
  notes: { text: string; action: boolean; short: string | null }[];
  taxDue: number;
  facts: { label: string; amount: number }[] | null;
}

export interface Pit8CAmounts {
  revenue: number;
  costs: number;
}

export interface DeclarationAccountLine {
  accountId: string;
  name: string;
  broker: string;
  treatment: string;
  revenue: number;
  costs: number;
  dividendsGross: number;
  dividendsWithheld: number;
  takesPit8C: boolean;
  pit8C: Pit8CAmounts | null;
  pit8CCoveredBy: string | null;
}

export interface DeclarationForm {
  code: string;
  title: string;
}

export function formValue(field: DeclarationField): string {
  if (field.amount === null) return field.text ?? '';
  return Number.isInteger(field.amount)
    ? String(field.amount)
    : field.amount.toFixed(2).replace('.', ',');
}
