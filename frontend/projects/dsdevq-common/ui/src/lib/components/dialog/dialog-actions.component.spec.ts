import {ComponentFixture, TestBed} from '@angular/core/testing';

import {DialogActionsComponent, type DialogActionsAlign} from './dialog-actions.component';

describe('DialogActionsComponent', () => {
  let fixture: ComponentFixture<DialogActionsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [DialogActionsComponent]}).compileComponents();
    fixture = TestBed.createComponent(DialogActionsComponent);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('applies justify-end by default', () => {
    const div: HTMLDivElement = fixture.nativeElement.querySelector('div');
    expect(div.className).toContain('justify-end');
  });

  it('applies justify-between when align="between"', () => {
    fixture.componentRef.setInput('align', 'between' as DialogActionsAlign);
    fixture.detectChanges();
    const div: HTMLDivElement = fixture.nativeElement.querySelector('div');
    expect(div.className).toContain('justify-between');
  });

  it('applies edge-to-edge negative margins to break out of dialog body padding', () => {
    const div: HTMLDivElement = fixture.nativeElement.querySelector('div');
    expect(div.className).toContain('-mx-cmn-6');
    expect(div.className).toContain('-mb-cmn-6');
  });

  it('applies top border separator', () => {
    const div: HTMLDivElement = fixture.nativeElement.querySelector('div');
    expect(div.className).toContain('border-t');
  });
});
