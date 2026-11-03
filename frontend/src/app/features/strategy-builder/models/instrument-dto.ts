export interface InstrumentChoiceDto {
  id: string;
  label: string;
  name: string;
  group: string | null;
  assetClass: string | null;
  region: string | null;
  isVolatilityIndex: boolean;
}
