import { ApiSchema } from '../../../core/api/api-schema';

export type NotificationDestination = ApiSchema<'DestinationDto'>;
export type NotificationSettings = ApiSchema<'NotificationSettingsDto'>;
export type ChangeNotificationSettingsBody = ApiSchema<'ChangeNotificationSettingsCommand'>;
