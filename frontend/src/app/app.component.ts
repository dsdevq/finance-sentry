import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {RouterOutlet} from '@angular/router';
import {ThemeService} from '@dsdevq-common/ui';

@Component({
  selector: 'fns-root',
  imports: [RouterOutlet],
  template: `
    <div class="fns-container">
      <header class="fns-header">
        <h1>Finance Sentry</h1>
        <div class="header-controls">
          <button
            [attr.aria-label]="'Switch to ' + (isDark ? 'light' : 'dark') + ' theme'"
            (click)="toggleTheme()"
            class="theme-toggle"
          >
            {{ isDark ? '☀️ Light' : '🌙 Dark' }}
          </button>
          <button (click)="setTestAccent()" class="theme-toggle" aria-label="Set red accent">
            Accent: Red
          </button>
          <button (click)="resetAccent()" class="theme-toggle" aria-label="Reset accent">
            Reset Accent
          </button>
        </div>
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
        display: flex;
        align-items: center;
        justify-content: space-between;
      }
      .header-controls {
        display: flex;
        gap: 0.5rem;
        align-items: center;
      }
      .theme-toggle {
        background: rgba(255, 255, 255, 0.15);
        border: 1px solid rgba(255, 255, 255, 0.4);
        color: white;
        padding: 0.25rem 0.75rem;
        border-radius: 4px;
        cursor: pointer;
        font-size: 0.875rem;
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
  private readonly themeService = inject(ThemeService);

  public title = 'Finance Sentry';

  public get isDark(): boolean {
    return this.themeService.getTheme() === 'dark';
  }

  public toggleTheme(): void {
    this.themeService.setTheme(this.isDark ? 'light' : 'dark');
  }

  public setTestAccent(): void {
    this.themeService.setAccent('#e11d48');
  }

  public resetAccent(): void {
    this.themeService.resetAccent();
  }
}
