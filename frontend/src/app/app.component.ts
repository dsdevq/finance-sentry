import { Component } from '@angular/core';

@Component({
  selector: 'app-root',
  template: `
    <div class="app-container">
      <header class="app-header">
        <h1>Finance Sentry</h1>
      </header>
      <main class="app-main">
        <router-outlet></router-outlet>
      </main>
    </div>
  `,
  styles: [`
    .app-container {
      min-height: 100vh;
      display: flex;
      flex-direction: column;
    }
    .app-header {
      background-color: #1976d2;
      color: white;
      padding: 1rem;
    }
    .app-main {
      flex: 1;
      padding: 1rem;
    }
  `]
})
export class AppComponent {
  public title: string = 'Finance Sentry';
}
