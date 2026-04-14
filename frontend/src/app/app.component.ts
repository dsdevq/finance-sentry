import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {RouterOutlet} from '@angular/router';
import {ButtonComponent, ThemeService} from '@dsdevq-common/ui';

@Component({
  selector: 'fns-root',
  imports: [RouterOutlet, ButtonComponent],
  template: `
    <div class="fns-container">
      <header class="fns-header">
        <h1>Finance Sentry</h1>
        <div class="header-controls">
          <cmn-button
            [attr.aria-label]="'Switch to ' + (isDark ? 'light' : 'dark') + ' theme'"
            (clicked)="toggleTheme()"
            variant="secondary"
            size="sm"
            >{{ isDark ? '☀ Light' : '🌙 Dark' }}</cmn-button
          >
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
        background-color: var(--color-accent-800);
        color: var(--color-text-inverse);
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
}
