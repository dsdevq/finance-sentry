import {platformBrowser} from '@angular/platform-browser';

import {AppComponent} from './app/app.component';

platformBrowser()
  .bootstrapModule(AppComponent)

  .catch(err => console.error(err));
