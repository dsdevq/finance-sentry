/* eslint-disable @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-member-access */
import {OverlayContainer} from '@angular/cdk/overlay';
import {type ComponentFixture, TestBed} from '@angular/core/testing';
import {By} from '@angular/platform-browser';
import {afterEach, beforeEach, describe, expect, it} from 'vitest';

import {MenuComponent, type MenuItem} from './menu.component';

const MENU_ITEMS: MenuItem[] = [
  {id: 'settings', label: 'Settings', icon: 'Settings2'},
  {id: 'logout', label: 'Log out', icon: 'LogOut', destructive: true},
];

describe('MenuComponent', () => {
  let fixture: ComponentFixture<MenuComponent>;
  let overlayContainer: OverlayContainer;

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [MenuComponent]}).compileComponents();
    fixture = TestBed.createComponent(MenuComponent);
    overlayContainer = TestBed.inject(OverlayContainer);
    fixture.componentRef.setInput('items', MENU_ITEMS);
    fixture.componentRef.setInput('ariaLabel', 'Account menu');
    fixture.detectChanges();
  });

  afterEach(() => {
    fixture.destroy();
    overlayContainer.ngOnDestroy();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should open when the trigger is clicked', () => {
    const trigger = fixture.debugElement.query(By.css('button'));
    trigger.triggerEventHandler('click', new MouseEvent('click'));
    fixture.detectChanges();

    expect(overlayContainer.getContainerElement().textContent).toContain('Settings');
    expect(overlayContainer.getContainerElement().textContent).toContain('Log out');
  });

  it('should emit the selected item and close the menu', () => {
    const selected: MenuItem[] = [];
    fixture.componentInstance.itemSelect.subscribe(item => selected.push(item));

    fixture.debugElement
      .query(By.css('button'))
      .triggerEventHandler('click', new MouseEvent('click'));
    fixture.detectChanges();

    const menuButtons = overlayContainer.getContainerElement().querySelectorAll('button');
    menuButtons[1]?.dispatchEvent(new MouseEvent('click', {bubbles: true}));
    fixture.detectChanges();

    expect(selected).toEqual([{id: 'logout', label: 'Log out', icon: 'LogOut', destructive: true}]);
    expect(overlayContainer.getContainerElement().textContent).toBe('');
  });

  it('should close on Escape', () => {
    fixture.debugElement
      .query(By.css('button'))
      .triggerEventHandler('click', new MouseEvent('click'));
    fixture.detectChanges();

    const overlayPane = overlayContainer.getContainerElement().querySelector('.cdk-overlay-pane');
    overlayPane?.dispatchEvent(new KeyboardEvent('keydown', {key: 'Escape', bubbles: true}));
    fixture.detectChanges();

    expect(overlayContainer.getContainerElement().textContent).toBe('');
  });
});
