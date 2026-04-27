import {provideHttpClient} from '@angular/common/http';
import {HttpTestingController, provideHttpClientTesting} from '@angular/common/http/testing';
import {TestBed} from '@angular/core/testing';
import {firstValueFrom} from 'rxjs';

import {CmnIconRegistry} from './icon-registry.service';

const SVG = '<svg><circle cx="0" cy="0" r="1"/></svg>';

describe('CmnIconRegistry', () => {
  let registry: CmnIconRegistry;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    registry = TestBed.inject(CmnIconRegistry);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('returns inline SVG synchronously without HTTP', async () => {
    registry.registerInline('local', SVG);
    const svg = await firstValueFrom(registry.resolve('local'));
    expect(svg).toBeTruthy();
  });

  it('returns null for an unknown name', async () => {
    const svg = await firstValueFrom(registry.resolve('missing'));
    expect(svg).toBeNull();
  });

  it('fetches a registered URL and caches subsequent calls', async () => {
    registry.registerUrl('remote', '/icons/remote.svg');

    const first = firstValueFrom(registry.resolve('remote'));
    const reqs = http.match('/icons/remote.svg');
    expect(reqs.length).toBe(1);
    reqs[0].flush(SVG);
    expect(await first).toBeTruthy();

    const second = await firstValueFrom(registry.resolve('remote'));
    expect(second).toBeTruthy();
  });

  it('registerInlineMap registers each entry', () => {
    registry.registerInlineMap({a: SVG, b: SVG});
    expect(registry.has('a')).toBe(true);
    expect(registry.has('b')).toBe(true);
  });

  it('registerUrlMap registers each entry', () => {
    registry.registerUrlMap({a: '/a.svg', b: '/b.svg'});
    expect(registry.has('a')).toBe(true);
    expect(registry.has('b')).toBe(true);
  });
});
