import {HttpClient} from '@angular/common/http';
import {inject, Injectable} from '@angular/core';
import {DomSanitizer, type SafeHtml} from '@angular/platform-browser';
import {map, type Observable, of, shareReplay, tap} from 'rxjs';

@Injectable({providedIn: 'root'})
export class CmnIconRegistry {
  private readonly http = inject(HttpClient);
  private readonly sanitizer = inject(DomSanitizer);

  private readonly inlineSvg = new Map<string, SafeHtml>();
  private readonly urls = new Map<string, string>();
  private readonly urlCache = new Map<string, Observable<SafeHtml>>();

  public registerInline(name: string, rawSvg: string): void {
    this.inlineSvg.set(name, this.sanitizer.bypassSecurityTrustHtml(rawSvg));
  }

  public registerUrl(name: string, url: string): void {
    this.urls.set(name, url);
  }

  public registerInlineMap(entries: Readonly<Record<string, string>>): void {
    for (const [name, svg] of Object.entries(entries)) {
      this.registerInline(name, svg);
    }
  }

  public registerUrlMap(entries: Readonly<Record<string, string>>): void {
    for (const [name, url] of Object.entries(entries)) {
      this.registerUrl(name, url);
    }
  }

  public has(name: string): boolean {
    return this.inlineSvg.has(name) || this.urls.has(name);
  }

  public resolve(name: string): Observable<SafeHtml | null> {
    const inline = this.inlineSvg.get(name);
    if (inline) {
      return of(inline);
    }
    const url = this.urls.get(name);
    if (!url) {
      return of(null);
    }
    const cached = this.urlCache.get(name);
    if (cached) {
      return cached;
    }
    const stream$ = this.http.get(url, {responseType: 'text'}).pipe(
      map(svg => this.sanitizer.bypassSecurityTrustHtml(svg)),
      tap(safe => this.inlineSvg.set(name, safe)),
      shareReplay({bufferSize: 1, refCount: false})
    );
    this.urlCache.set(name, stream$);
    return stream$;
  }
}
