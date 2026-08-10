import {
  ChangeDetectionStrategy,
  Component,
  computed,
  CUSTOM_ELEMENTS_SCHEMA,
  input,
  signal,
} from '@angular/core';
import {type Subscription} from 'rxjs';

import {type CmnChatMessage, type CmnChatStreamFn} from './chat.model';

/** Minimal shape of the Deep Chat streaming signals passed to a custom `connect.handler`. */
interface DeepChatSignals {
  onOpen: () => void;
  onClose: () => void;
  onResponse: (response: {text?: string; error?: string}) => void;
  stopClicked: {listener: () => void};
}

interface DeepChatRequestBody {
  messages?: {role?: string; text?: string}[];
}

interface DeepChatConnect {
  stream: boolean;
  handler: (body: DeepChatRequestBody, signals: DeepChatSignals) => void;
}

const DEFAULT_ERROR = 'Something went wrong.';
const UNAVAILABLE_ERROR = 'The agent is unavailable right now. Please try again.';

/**
 * Thin wrapper over the framework-agnostic <a href="https://deepchat.dev">Deep Chat</a> web component
 * — it owns the message list, markdown rendering, streaming cursor, auto-scroll and input, themed to
 * the app's design tokens. The host supplies preloaded {@link history} and a {@link stream} function
 * for each turn; conversation-id threading and transport stay in the host. `deep-chat` renders in a
 * shadow root, so CSS custom properties (`var(--color-*)`) inherit through and keep it theme-aware.
 *
 * The `<deep-chat>` custom element is registered by a lazy `import()` in the constructor, so its
 * ~400&nbsp;kB bundle is a separate chunk fetched only when a chat surface first mounts. Crucially the
 * element is rendered only AFTER that import resolves (`ready`): Deep Chat doesn't reclaim properties
 * set before upgrade, so binding `connect`/`introMessage`/`messageStyles` to a not-yet-defined element
 * would be silently dropped and it would boot with its default (unthemed, no-connect) demo state. The
 * self-contained bundle also sidesteps Deep Chat's package `.d.ts`, whose transitive `speech-to-element`
 * subpath type won't resolve here.
 */
@Component({
  selector: 'cmn-chat',
  changeDetection: ChangeDetectionStrategy.OnPush,
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  host: {class: 'block h-full w-full min-h-0'},
  template: `
    @if (ready()) {
      <deep-chat
        [style.width]="'100%'"
        [style.height]="'100%'"
        [style.border]="'none'"
        [style.borderRadius]="'0'"
        [style.backgroundColor]="'transparent'"
        [connect]="connect"
        [history]="history()"
        [introMessage]="introMessageObj()"
        [messageStyles]="messageStyles"
        [textInput]="textInput()"
        [inputAreaStyle]="inputAreaStyle"
        [submitButtonStyles]="submitButtonStyles"
        [auxiliaryStyle]="auxiliaryStyle"
      />
    }
  `,
})
export class ChatComponent {
  public readonly history = input<CmnChatMessage[]>([]);
  public readonly stream = input.required<CmnChatStreamFn>();
  public readonly placeholder = input<string>('Ask…');
  public readonly introMessage = input<string>('');

  // Built once; the handler reads `stream()` lazily so each turn uses the current transport closure.
  public readonly connect: DeepChatConnect = {
    stream: true,
    handler: (body, signals): void => this.runTurn(body, signals),
  };

  public readonly messageStyles = {
    default: {
      shared: {bubble: {maxWidth: '85%', fontSize: '0.9rem', lineHeight: '1.55'}},
      user: {
        bubble: {
          backgroundColor: 'var(--color-accent-default)',
          color: 'var(--color-text-inverse)',
        },
      },
      ai: {
        bubble: {
          backgroundColor: 'var(--color-surface-raised)',
          color: 'var(--color-text-primary)',
        },
      },
      loading: {
        bubble: {
          backgroundColor: 'var(--color-surface-raised)',
          color: 'var(--color-text-secondary)',
        },
      },
    },
  };

  public readonly inputAreaStyle = {
    backgroundColor: 'var(--color-surface-card)',
    borderTop: '1px solid var(--color-border-default)',
  };

  public readonly submitButtonStyles = {
    submit: {
      container: {
        default: {backgroundColor: 'var(--color-accent-default)', borderRadius: '8px'},
      },
      svg: {styles: {default: {filter: 'brightness(0) invert(1)'}}},
    },
  };

  public readonly auxiliaryStyle = `
    ::-webkit-scrollbar { width: 8px; }
    ::-webkit-scrollbar-thumb { background-color: var(--color-border-strong); border-radius: 4px; }
    ::-webkit-scrollbar-track { background: transparent; }
  `;

  public readonly textInput = computed(() => ({
    placeholder: {text: this.placeholder()},
    styles: {
      text: {color: 'var(--color-text-primary)'},
      container: {
        backgroundColor: 'var(--color-surface-bg)',
        border: '1px solid var(--color-border-default)',
        borderRadius: '10px',
        boxShadow: 'none',
      },
    },
  }));

  public readonly introMessageObj = computed(() =>
    this.introMessage() ? {text: this.introMessage()} : undefined
  );

  // Only true once <deep-chat> is defined, so bindings hit an upgraded element (see class docs).
  protected readonly ready = signal(false);

  constructor() {
    // Register the <deep-chat> element on demand — a separate lazy chunk (see class docs).
    // @ts-expect-error - the self-contained bundle ships no type declarations; used only for its
    // custom-element registration side effect, so an untyped module is correct here.
    void import('deep-chat/dist/deepChat.bundle.js').then(() => this.ready.set(true));
  }

  private runTurn(body: DeepChatRequestBody, signals: DeepChatSignals): void {
    const messages = body.messages ?? [];
    const last = messages[messages.length - 1];
    const text = typeof last?.text === 'string' ? last.text : '';

    const {stopClicked} = signals;
    let opened = false;
    const subscription: Subscription = this.stream()(text).subscribe({
      next: event => {
        if (event.type === 'text') {
          if (!opened) {
            signals.onOpen();
            opened = true;
          }
          signals.onResponse({text: event.delta});
        } else {
          signals.onResponse({error: event.message ?? DEFAULT_ERROR});
        }
      },
      error: () => {
        signals.onResponse({error: UNAVAILABLE_ERROR});
        signals.onClose();
      },
      complete: () => {
        if (!opened) {
          signals.onOpen();
        }
        signals.onClose();
      },
    });

    stopClicked.listener = (): void => subscription.unsubscribe();
  }
}
