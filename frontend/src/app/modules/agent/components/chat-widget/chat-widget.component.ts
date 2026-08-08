import {
  afterRenderEffect,
  ChangeDetectionStrategy,
  Component,
  computed,
  type ElementRef,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import {toSignal} from '@angular/core/rxjs-interop';
import {NavigationEnd, Router} from '@angular/router';
import {
  AlertComponent,
  ChatInputComponent,
  ChatMessageComponent,
  EmptyStateComponent,
  IconComponent,
  type LucideIconName,
} from '@dsdevq-common/ui';
import {filter, map} from 'rxjs';

import {AppRoute} from '../../../../shared/enums/app-route/app-route.enum';
import {AgentChatStore} from '../../store/agent-chat.store';

@Component({
  selector: 'fns-chat-widget',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AgentChatStore],
  imports: [
    AlertComponent,
    ChatInputComponent,
    ChatMessageComponent,
    EmptyStateComponent,
    IconComponent,
  ],
  templateUrl: './chat-widget.component.html',
})
export class ChatWidgetComponent {
  private readonly scrollContainer = viewChild<ElementRef<HTMLElement>>('scrollContainer');
  private readonly router = inject(Router);
  private readonly routerUrl = toSignal(
    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd),
      map(e => e.urlAfterRedirects)
    ),
    {initialValue: this.router.url}
  );

  public readonly store = inject(AgentChatStore);
  public readonly isOpen = signal(false);
  // The full-page Ledger already hosts a chat — the floating widget would be redundant there.
  public readonly isHidden = computed(() => this.routerUrl().startsWith(AppRoute.Ledger));

  protected readonly openIcon: LucideIconName = 'Sparkles';
  protected readonly closeIcon: LucideIconName = 'X';
  protected readonly newChatIcon: LucideIconName = 'Plus';
  protected readonly emptyIcon: LucideIconName = 'Sparkles';

  constructor() {
    // Keep the newest message in view as text streams in (a render concern, not state).
    afterRenderEffect(() => {
      this.store.messages();
      if (!this.isOpen()) {
        return;
      }
      const element = this.scrollContainer()?.nativeElement;
      if (element) {
        element.scrollTop = element.scrollHeight;
      }
    });
  }

  public toggle(): void {
    this.isOpen.update(open => !open);
  }

  public onSend(text: string): void {
    this.store.send(text);
  }

  public onNewChat(): void {
    this.store.resetThread();
  }
}
