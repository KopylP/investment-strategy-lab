/** A yearly return as the server fills it from all of the user's accounts (PIT-38 for Polish ones). */
export interface DeclarationField {
  /** The form's position number ("poz."). */
  position: number;
  label: string;
  /** Null for a text field. */
  amount: number | null;
  text: string | null;
  hint: string | null;
  /** The online form works it out from the others: to check, not to type. */
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
}

/** Revenue (poz. 35) and costs (poz. 36) on a broker's PIT-8C. */
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
  /** Row 1: the broker sends PIT-8C, whose amounts stand instead of ours once entered. */
  takesPit8C: boolean;
  pit8C: Pit8CAmounts | null;
}

export interface DeclarationForm {
  code: string;
  title: string;
}

/** The value as a Polish form takes it: comma decimals, no grouping, whole numbers without grosze. */
export function formValue(field: DeclarationField): string {
  if (field.amount === null) return field.text ?? '';
  return Number.isInteger(field.amount) ? String(field.amount) : field.amount.toFixed(2).replace('.', ',');
}
