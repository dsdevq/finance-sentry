import {inject, Injectable, InjectionToken} from '@angular/core';
import chroma from 'chroma-js';
import {BehaviorSubject, Observable} from 'rxjs';

export type Theme = 'light' | 'dark';

const THEME_STORAGE_KEY = 'cmn-theme';
const ACCENT_STORAGE_KEY = 'cmn-accent';
const DEFAULT_THEME: Theme = 'light';

/** Number of accent palette stops (100 → 1000). */
const ACCENT_STOP_COUNT = 11;
/** Multiplier from stop index to CSS property suffix (1 → 100, 2 → 200, …). */
const ACCENT_STOP_MULTIPLIER = 100;
/** Minimum WCAG AA contrast ratio for text on backgrounds. */
const MIN_WCAG_AA_CONTRAST = 4.5;
/** Step toward midrange when auto-correcting a failing stop. */
const AUTO_CORRECT_STEP = 0.05;
/** Iteration limit for contrast auto-correction. */
const MAX_CORRECT_ITERATIONS = 20;
/** Lightness midpoint for OkLCH auto-correction direction. */
const OKLCH_MIDPOINT = 0.5;
/** Fallback surface background color used when CSS variable is not yet set. */
const FALLBACK_SURFACE_BG = '#f8f9fa';

export const DOCUMENT = new InjectionToken<Document>('DOCUMENT', {
  providedIn: 'root',
  factory: () => document,
});

export const LOCAL_STORAGE = new InjectionToken<Storage>('LOCAL_STORAGE', {
  providedIn: 'root',
  factory: () => localStorage,
});

@Injectable({providedIn: 'root'})
export class ThemeService {
  private readonly doc = inject(DOCUMENT);
  private readonly storage = inject(LOCAL_STORAGE);

  private readonly themeSubject$: BehaviorSubject<Theme>;
  private readonly accentSubject$: BehaviorSubject<string | null>;

  public readonly activeTheme$: Observable<Theme>;
  public readonly activeAccent$: Observable<string | null>;

  constructor() {
    const stored = this.storage.getItem(THEME_STORAGE_KEY) as Theme | null;
    const initial: Theme = stored === 'dark' ? 'dark' : DEFAULT_THEME;
    this.themeSubject$ = new BehaviorSubject<Theme>(initial);
    this.activeTheme$ = this.themeSubject$.asObservable();
    this.applyTheme(initial);

    const storedAccent = this.storage.getItem(ACCENT_STORAGE_KEY);
    this.accentSubject$ = new BehaviorSubject<string | null>(storedAccent);
    this.activeAccent$ = this.accentSubject$.asObservable();
    if (storedAccent) {
      this.applyAccentPalette(storedAccent);
    }
  }

  // ── Theme ─────────────────────────────────────────────────────────────────

  public setTheme(theme: Theme): void {
    this.applyTheme(theme);
    this.storage.setItem(THEME_STORAGE_KEY, theme);
    this.themeSubject$.next(theme);
  }

  public getTheme(): Theme {
    return this.themeSubject$.value;
  }

  private applyTheme(theme: Theme): void {
    this.doc.documentElement.setAttribute('data-theme', theme);
  }

  // ── Accent color ──────────────────────────────────────────────────────────

  /**
   * Generate an 11-stop OkLCH palette from `hex`, auto-correct stops that
   * fail WCAG AA contrast against `--color-surface-bg`, write all stops as
   * `--cmn-accent-N` CSS custom properties, and persist the hex to localStorage.
   */
  public setAccent(hex: string): void {
    this.applyAccentPalette(hex);
    this.storage.setItem(ACCENT_STORAGE_KEY, hex);
    this.accentSubject$.next(hex);
  }

  /**
   * Remove all inline `--cmn-accent-*` custom properties and clear localStorage.
   * The CSS file fallbacks (`var(--color-accent-*)`) take over immediately.
   */
  public resetAccent(): void {
    for (let stop = 1; stop <= ACCENT_STOP_COUNT; stop++) {
      this.doc.documentElement.style.removeProperty(
        `--cmn-accent-${stop * ACCENT_STOP_MULTIPLIER}`
      );
    }
    this.storage.removeItem(ACCENT_STORAGE_KEY);
    this.accentSubject$.next(null);
  }

  public getStoredAccent(): string | null {
    return this.storage.getItem(ACCENT_STORAGE_KEY);
  }

  private applyAccentPalette(hex: string): void {
    const stops = this.buildPalette(hex);
    const bgVar =
      this.doc.documentElement.style.getPropertyValue('--color-surface-bg') || FALLBACK_SURFACE_BG;

    for (let i = 0; i < stops.length; i++) {
      const stop = (i + 1) * ACCENT_STOP_MULTIPLIER;
      const corrected = this.autoCorrectContrast(stops[i], bgVar);
      this.doc.documentElement.style.setProperty(`--cmn-accent-${stop}`, corrected);
    }
  }

  private buildPalette(hex: string): string[] {
    return chroma.scale(['#f8f8f8', hex, '#0a0a0a']).mode('oklch').colors(ACCENT_STOP_COUNT);
  }

  private autoCorrectContrast(color: string, background: string): string {
    let c = chroma(color);
    let iterations = 0;
    while (
      chroma.contrast(c, background) < MIN_WCAG_AA_CONTRAST &&
      iterations < MAX_CORRECT_ITERATIONS
    ) {
      const l = c.get('oklch.l');
      const target = l < OKLCH_MIDPOINT ? l - AUTO_CORRECT_STEP : l + AUTO_CORRECT_STEP;
      c = c.set('oklch.l', Math.max(0, Math.min(1, target)));
      iterations++;
    }
    return c.hex();
  }
}
