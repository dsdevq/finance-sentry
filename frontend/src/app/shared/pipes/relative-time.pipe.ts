import {Pipe, type PipeTransform} from '@angular/core';

import {TimeUtils} from '../utils/time.utils';

@Pipe({name: 'relativeTime'})
export class RelativeTimePipe implements PipeTransform {
  public transform(timestamp: Nullable<string>): string {
    return TimeUtils.getRelativeTime(timestamp);
  }
}
