import {By} from '@angular/platform-browser';
import {type ComponentFixture, TestBed} from '@angular/core/testing';

import {TopBarComponent} from './top-bar.component';

describe('TopBarComponent', () => {
  let fixture: ComponentFixture<TopBarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [TopBarComponent]}).compileComponents();
    fixture = TestBed.createComponent(TopBarComponent);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should display the title', () => {
    fixture.componentRef.setInput('title', 'Dashboard');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Dashboard');
  });

  it('should emit searchClick when search button is clicked', () => {
    const emitted: void[] = [];
    fixture.componentInstance.searchClick.subscribe(() => emitted.push(undefined));
    const btn = fixture.debugElement.queryAll(By.css('button'))[0];
    btn.triggerEventHandler('click', null);
    expect(emitted.length).toBe(1);
  });

  it('should show avatar initial from avatarLabel', () => {
    fixture.componentRef.setInput('avatarLabel', 'Denys');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('D');
  });
});
