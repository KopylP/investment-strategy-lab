import { components } from './generated/backend-api';

export type ApiSchema<Name extends keyof components['schemas']> = components['schemas'][Name];
