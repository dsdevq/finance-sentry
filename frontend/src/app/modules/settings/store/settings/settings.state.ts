import {type UserProfile} from '../../models/settings/settings.model';

export interface SettingsState {
  profile: Nullable<UserProfile>;
  profileSaving: boolean;
  passwordSaving: boolean;
  showDeleteConfirm: boolean;
  status: AsyncStatus;
}

export const initialSettingsState: SettingsState = {
  profile: null,
  profileSaving: false,
  passwordSaving: false,
  showDeleteConfirm: false,
  status: 'idle',
};
