import { ApiSchema } from '../../../core/api/api-schema';

export type ImportMode = 'Merge' | 'UpdateImported' | 'Replace' | 'FromScratch';
export type ImportStatus = 'Preview' | 'Confirmed' | 'Undone';
export type RowDecision =
  | 'New'
  | 'AlreadyImported'
  | 'Differs'
  | 'MatchesExisting'
  | 'OutOfScope'
  | 'Unsupported'
  | 'NeedsMapping';

export type TickerMapping = ApiSchema<'TickerMappingDto'>;

export type StatementRow = Omit<ApiSchema<'StatementRowDto'>, 'decision'> & {
  decision: RowDecision;
};

export type StatementImport = Omit<ApiSchema<'StatementImportDto'>, 'status' | 'mode' | 'rows'> & {
  status: ImportStatus;
  mode: ImportMode;
  rows: StatementRow[];
};

export type ImportSummary = ApiSchema<'StatementImportSummaryDto'>;
export type UploadStatementBody = ApiSchema<'UploadStatementRequest'>;
export type ConfigureImportBody = ApiSchema<'ConfigureImportCommand'>;
