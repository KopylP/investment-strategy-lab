import { ApiSchema } from '../../../core/api/api-schema';

export type TaxPolicyDto = ApiSchema<'TaxPolicyDto'>;
export type NewSubPortfolioDto = ApiSchema<'NewSubPortfolioDto'>;
export type OpenAccountBody = ApiSchema<'OpenAccountCommand'>;
export type ChangeTargetSharesBody = ApiSchema<'ChangeTargetSharesCommand'>;
export type ContributionPolicyChoice = ApiSchema<'ContributionPolicyChoice'>;
export type AssignStrategyBody = ApiSchema<'AssignStrategyCommand'>;
export type AddSubPortfolioBody = ApiSchema<'AddSubPortfolioCommand'>;
export type RenameSubPortfolioBody = ApiSchema<'RenameSubPortfolioCommand'>;
export type RetireSubPortfolioBody = ApiSchema<'RetireSubPortfolioCommand'>;
export type PlanSettingsDto = ApiSchema<'PlanSettingsDto'>;
export type ChangePlanSettingsBody = ApiSchema<'ChangePlanSettingsCommand'>;
export type ImportReport = ApiSchema<'ImportReport'>;
