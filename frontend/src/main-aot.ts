import {AppComponent} from './app/app.component';
import {platformBrowser} from '@angular/platform-browser';

platformBrowser()
  .bootstrapModule(AppComponent)
  // eslint-disable-next-line no-console
  .catch((err) => console.error(err));
