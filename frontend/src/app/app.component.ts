import {ChangeDetectionStrategy, Component} from '@angular/core';
import {RouterOutlet} from '@angular/router';

@Component({
  selector: 'fns-root',
  imports: [RouterOutlet],
  template: `
    <div class="fns-container">
      <header class="fns-header">
        <h1>Finance Sentry</h1>
      </header>
      <main class="fns-main">
        <router-outlet />
      </main>
    </div>
  `,
  styles: [
    `
      .fns-container {
        min-height: 100vh;
        display: flex;
        flex-direction: column;
      }
      .fns-header {
        background-color: #1976d2;
        color: white;
        padding: 1rem;
      }
      .fns-main {
        flex: 1;
        padding: 1rem;
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppComponent {
  public title = 'Finance Sentry';
}
