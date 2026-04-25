import {By} from '@angular/platform-browser';
import {type ComponentFixture, TestBed} from '@angular/core/testing';

import {AppLayoutComponent, type NavItem} from './app-layout.component';

const NAV_ITEMS: NavItem[] = [
  {label: 'Dashboard', icon: 'LayoutDashboard', route: '/dashboard'},
  {label: 'Accounts', icon: 'Building2', route: '/accounts'},
];

describe('AppLayoutComponent', () => {
  let fixture: ComponentFixture<AppLayoutComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [AppLayoutComponent]}).compileComponents();
    fixture = TestBed.createComponent(AppLayoutComponent);
    fixture.componentRef.setInput('navItems', NAV_ITEMS);
    fixture.componentRef.setInput('activeRoute', '/dashboard');
    fixture.componentRef.setInput('title', 'Dashboard');
    fixture.componentRef.setInput('avatarLabel', 'D');
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render the sidebar', () => {
    const sidebar = fixture.debugElement.query(By.css('cmn-sidebar-nav'));
    expect(sidebar).toBeTruthy();
  });

  it('should render the top bar with title', () => {
    expect(fixture.nativeElement.textContent).toContain('Dashboard');
  });

  it('should emit navClick when a nav item is clicked', () => {
    const emitted: NavItem[] = [];
    fixture.componentInstance.navClick.subscribe(item => emitted.push(item));
    const navButtons = fixture.debugElement.queryAll(By.css('cmn-sidebar-nav button'));
    navButtons[1]?.triggerEventHandler('click', null);
    expect(emitted.length).toBe(1);
  });

  it('should emit themeToggle from top bar', () => {
    const emitted: void[] = [];
    fixture.componentInstance.themeToggle.subscribe(() => emitted.push(undefined));
    const topBarButtons = fixture.debugElement.queryAll(By.css('cmn-top-bar button'));
    const themeBtn = topBarButtons[1];
    themeBtn?.triggerEventHandler('click', null);
    expect(emitted.length).toBe(1);
  });
});
