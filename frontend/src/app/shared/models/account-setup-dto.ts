import { ApiSchema } from '../../core/api/api-schema';

export type ContributionPolicyKind = 'FillUnderweight' | 'FixedSplit';
export type SubPortfolioStatus = 'Active' | 'Retired';

export type SubPortfolioSetup = Omit<ApiSchema<'SubPortfolioSetupDto'>, 'status'> & {
  status: SubPortfolioStatus;
};

export type AccountSetup = Omit<
  ApiSchema<'AccountSetupDto'>,
  'contributionPolicy' | 'subPortfolios'
> & {
  contributionPolicy: ContributionPolicyKind;
  subPortfolios: SubPortfolioSetup[];
};
