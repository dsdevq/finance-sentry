import {type ComponentFixture, TestBed} from '@angular/core/testing';

import type {NavItem} from './sidebar-nav.component';
import {SidebarNavComponent} from './sidebar-nav.component';

const ITEMS: NavItem[] = [
  {label: 'Dashboard', icon: 'LayoutDashboard', route: '/dashboard'},
  {label: 'Accounts', icon: 'Building2', route: '/accounts'},
];

describe('SidebarNavComponent', () => {
  let fixture: ComponentFixture<SidebarNavComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [SidebarNavComponent]}).compileComponents();
    fixture = TestBed.createComponent(SidebarNavComponent);
    fixture.componentRef.setInput('items', ITEMS);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render nav item labels when expanded', () => {
    const text: string = fixture.nativeElement.textContent;
    expect(text).toContain('Dashboard');
    expect(text).toContain('Accounts');
  });

  it('should collapse when toggle is clicked', () => {
    const btn: HTMLButtonElement = fixture.nativeElement.querySelector(
      'button[title="Collapse sidebar"]'
    );
    btn?.click();
    fixture.detectChanges();
    expect(fixture.componentInstance.collapsed()).toBe(true);
  });

  it('should emit navClick when a nav item is clicked', () => {
    const emitted: NavItem[] = [];
    fixture.componentInstance.navClick.subscribe((item: NavItem) => emitted.push(item));
    const buttons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('nav button');
    buttons[0]?.click();
    expect(emitted.length).toBe(1);
    expect(emitted[0].route).toBe('/dashboard');
  });
});
