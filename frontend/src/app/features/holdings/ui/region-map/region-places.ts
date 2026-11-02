export interface RegionPlace {
  longitude: number;
  latitude: number;
}

export const REGION_PLACES: Record<string, RegionPlace> = {
  UnitedStates: { longitude: -98, latitude: 39 },
  Eurozone: { longitude: 10, latitude: 49 },
  DevelopedExUnitedStates: { longitude: -2, latitude: 56 },
  Japan: { longitude: 138, latitude: 37 },
  EmergingMarkets: { longitude: 92, latitude: 22 },
};
