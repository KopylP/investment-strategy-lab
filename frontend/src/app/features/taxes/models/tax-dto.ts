import { ApiSchema } from '../../../core/api/api-schema';

export type TaxYearSummary = ApiSchema<'TaxYearSummaryDto'>;
export type Realization = ApiSchema<'RealizationDto'>;
export type TaxAccount = ApiSchema<'TaxAccountDto'>;
export type TaxOverview = ApiSchema<'TaxOverviewDto'>;
export type BrokerStatement = ApiSchema<'BrokerStatementDto'>;
export type BrokerStatements = ApiSchema<'BrokerStatementsDto'>;
export type TaxYearDetails = ApiSchema<'TaxYearDetailsDto'>;
export type LossCarryforward = ApiSchema<'LossCarryforwardDto'>;
export type RecordLossBody = ApiSchema<'RecordLossCommand'>;
export type UploadBrokerStatementBody = ApiSchema<'UploadBrokerStatementCommand'>;
