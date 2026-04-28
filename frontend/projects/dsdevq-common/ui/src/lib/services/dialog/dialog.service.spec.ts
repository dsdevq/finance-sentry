import {Dialog} from '@angular/cdk/dialog';
import {ChangeDetectionStrategy, Component} from '@angular/core';
import {TestBed} from '@angular/core/testing';

import {CmnDialogContainerComponent} from '../../components/dialog/dialog-container.component';
import {CmnDialogService} from './dialog.service';

@Component({
  selector: 'cmn-test-host',
  template: '<p>hello</p>',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
class HostComponent {}

describe('CmnDialogService', () => {
  let service: CmnDialogService;
  let openSpy: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(CmnDialogService);
    const dialog = TestBed.inject(Dialog);
    openSpy = vi.spyOn(dialog, 'open').mockReturnValue({close: vi.fn()} as never);
  });

  it('uses CmnDialogContainerComponent as the container', () => {
    service.open(HostComponent, {title: 'Hello'});
    const cfg = openSpy.mock.calls[0][1];
    expect(cfg.container).toBe(CmnDialogContainerComponent);
  });

  it('defaults size to md and applies the size panel class', () => {
    service.open(HostComponent);
    const cfg = openSpy.mock.calls[0][1];
    expect(cfg.panelClass).toContain('cmn-dialog-panel--md');
    expect(cfg.backdropClass).toBe('cmn-dialog-backdrop');
  });

  it('honours an explicit size override', () => {
    service.open(HostComponent, {size: 'sm'});
    const cfg = openSpy.mock.calls[0][1];
    expect(cfg.panelClass).toContain('cmn-dialog-panel--sm');
  });

  it('passes through data and title from the open config', () => {
    const data = {x: 1};
    service.open(HostComponent, {data, title: 'Title', disableClose: true});
    const cfg = openSpy.mock.calls[0][1];
    expect(cfg.data).toBe(data);
    expect(cfg.title).toBe('Title');
    expect(cfg.disableClose).toBe(true);
  });

  it('default disableClose is false; default autoFocus is "first-tabbable"', () => {
    service.open(HostComponent);
    const cfg = openSpy.mock.calls[0][1];
    expect(cfg.disableClose).toBe(false);
    expect(cfg.autoFocus).toBe('first-tabbable');
  });
});
