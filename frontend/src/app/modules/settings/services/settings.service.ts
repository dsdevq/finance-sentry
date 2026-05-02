import {Injectable} from '@angular/core';
import {ApiService} from '@dsdevq-common/core';
import {type Observable} from 'rxjs';

import {type UpdateProfileRequest, type UserProfile} from '../models/settings/settings.model';

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

@Injectable({providedIn: 'root'})
export class SettingsService extends ApiService {
  constructor() {
    super('profile');
  }

  public getProfile(): Observable<UserProfile> {
    return this.get<UserProfile>();
  }

  public updateProfile(request: UpdateProfileRequest): Observable<UserProfile> {
    return this.put<UserProfile>('', request);
  }

  public changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.post<void>('change-password', request);
  }
}
