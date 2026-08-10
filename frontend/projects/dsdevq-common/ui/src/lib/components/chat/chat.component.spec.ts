import {type ComponentRef} from '@angular/core';
import {type ComponentFixture, TestBed} from '@angular/core/testing';
import {EMPTY, of, Subject, throwError} from 'rxjs';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {ChatComponent} from './chat.component';
import {type CmnChatStreamEvent} from './chat.model';

type Handler = ChatComponent['connect']['handler'];
type Signals = Parameters<Handler>[1];
type Body = Parameters<Handler>[0];

function fakeSignals() {
  const signals = {
    onOpen: vi.fn(),
    onClose: vi.fn(),
    onResponse: vi.fn(),
    stopClicked: {listener: (): void => undefined},
  };
  return signals as Signals & typeof signals;
}

const BODY: Body = {messages: [{role: 'user', text: 'hi'}]};

describe('ChatComponent — streaming adapter', () => {
  let fixture: ComponentFixture<ChatComponent>;
  let ref: ComponentRef<ChatComponent>;

  beforeEach(() => {
    fixture = TestBed.createComponent(ChatComponent);
    ref = fixture.componentRef;
  });

  it('opens once, streams text deltas, then closes on completion', () => {
    const deltas: CmnChatStreamEvent[] = [
      {type: 'text', delta: 'Hel'},
      {type: 'text', delta: 'lo'},
    ];
    ref.setInput('stream', () => of(...deltas));
    const signals = fakeSignals();

    fixture.componentInstance.connect.handler(BODY, signals);

    expect(signals.onOpen).toHaveBeenCalledOnce();
    expect(signals.onResponse).toHaveBeenNthCalledWith(1, {text: 'Hel'});
    expect(signals.onResponse).toHaveBeenNthCalledWith(2, {text: 'lo'});
    expect(signals.onClose).toHaveBeenCalledOnce();
  });

  it('forwards a stream error event as a Deep Chat error response', () => {
    const error: CmnChatStreamEvent = {type: 'error', message: 'boom'};
    ref.setInput('stream', () => of(error));
    const signals = fakeSignals();

    fixture.componentInstance.connect.handler(BODY, signals);

    expect(signals.onResponse).toHaveBeenCalledWith({error: 'boom'});
  });

  it('surfaces a transport failure and closes the turn', () => {
    ref.setInput('stream', () => throwError(() => new Error('down')));
    const signals = fakeSignals();

    fixture.componentInstance.connect.handler(BODY, signals);

    expect(signals.onResponse).toHaveBeenCalledWith({
      error: 'The agent is unavailable right now. Please try again.',
    });
    expect(signals.onClose).toHaveBeenCalledOnce();
  });

  it('still opens and closes when the assistant stays silent (no deltas)', () => {
    ref.setInput('stream', () => EMPTY);
    const signals = fakeSignals();

    fixture.componentInstance.connect.handler(BODY, signals);

    expect(signals.onOpen).toHaveBeenCalledOnce();
    expect(signals.onClose).toHaveBeenCalledOnce();
  });

  it('stopping the turn unsubscribes so no further deltas render', () => {
    const source = new Subject<CmnChatStreamEvent>();
    ref.setInput('stream', () => source.asObservable());
    const signals = fakeSignals();

    fixture.componentInstance.connect.handler(BODY, signals);
    source.next({type: 'text', delta: 'first'});
    signals.stopClicked.listener();
    source.next({type: 'text', delta: 'second'});

    expect(signals.onResponse).toHaveBeenCalledTimes(1);
    expect(signals.onResponse).toHaveBeenCalledWith({text: 'first'});
  });
});
