# Drawer Service, TxDrawer, Holdings Positions Tab, Password Strength Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a reusable CDK-based `CmnDrawerService`, use it for a read-only transaction detail drawer, add a Positions tab to Holdings with real symbol/qty data and mock P&L%, and extract the password strength bars already in the register template into a proper `CmnPasswordStrengthComponent`.

**Architecture:** `CmnDrawerService` wraps CDK Overlay with a right-side slide-in `CmnDrawerContainerComponent` and a `CmnDrawerRef`; all four features consume library-first components per project rules. Holdings positions lazy-load from existing `/brokerage/holdings` and `/crypto/holdings` endpoints when the Positions tab is first activated.

**Tech Stack:** Angular 21, `@angular/cdk/overlay`, `@angular/cdk/portal`, `@ngrx/signals`, `@dsdevq-common/ui`, Tailwind CSS v3.

---

## File Map

### New — UI Library (`frontend/projects/dsdevq-common/ui/src/lib/`)
| File | Purpose |
|---|---|
| `components/drawer/drawer-config.ts` | `CMN_DRAWER_DATA` token, `CmnDrawerOpenConfig<D>` interface |
| `components/drawer/drawer-ref.ts` | `CmnDrawerRef<R>` — `close()`, `afterClosed()`, backdrop/ESC wiring |
| `components/drawer/drawer-container.component.ts` | Shell: header + scrollable body with `cdkPortalOutlet`, CSS slide animation |
| `services/drawer/drawer.service.ts` | `CmnDrawerService` — creates overlay, attaches container, attaches content portal |
| `components/password-strength/password-strength.component.ts` | `CmnPasswordStrengthComponent` — 4 bars + label, `score` input 0-4 |

### Modify — UI Library
| File | Change |
|---|---|
| `src/lib/index.ts` | Export all new drawer/password-strength types |
| `src/styles/theme.css` | Drawer CSS: backdrop, panel positioning, slide animation |

### New — Host App
| File | Purpose |
|---|---|
| `bank-sync/components/transaction-drawer/transaction-drawer.component.ts` | Read-only TxDrawer content component |
| `holdings/models/position/position.model.ts` | `Position` interface + `BrokerageHoldingsDto` / `CryptoHoldingsDto` DTOs |
| `holdings/services/positions.service.ts` | HTTP calls to `/brokerage/holdings` and `/crypto/holdings` |

### Modify — Host App
| File | Change |
|---|---|
| `bank-sync/pages/transaction-ledger/transaction-ledger.component.ts` | Add `openDrawer(tx)`, inject `CmnDrawerService` |
| `bank-sync/pages/transaction-ledger/transaction-ledger.component.html` | Add `(click)="openDrawer(t)"` on `<tr>` |
| `holdings/store/holdings.state.ts` | Add `positions`, `positionsStatus`, `positionsErrorCode` |
| `holdings/store/holdings.methods.ts` | Add `setPositionsLoading`, `setPositions`, `setPositionsError` |
| `holdings/store/holdings.computed.ts` | Add `positions`, `isPositionsLoading`, `positionsErrorMessage` |
| `holdings/store/holdings.effects.ts` | Add `loadPositions` rxMethod |
| `holdings/pages/holdings/holdings.component.ts` | Add `activeTab` signal, inject `CmnDrawerService`, import new components |
| `holdings/pages/holdings/holdings.component.html` | Tab bar + conditional Positions table |
| `auth/pages/register/register.component.ts` | Import `CmnPasswordStrengthComponent` |
| `auth/pages/register/register.component.html` | Replace inline bars with `<cmn-password-strength>` |

---

## Task 1: Drawer config, ref, and CSS

**Files:**
- Create: `frontend/projects/dsdevq-common/ui/src/lib/components/drawer/drawer-config.ts`
- Create: `frontend/projects/dsdevq-common/ui/src/lib/components/drawer/drawer-ref.ts`
- Modify: `frontend/projects/dsdevq-common/ui/src/styles/theme.css`

- [ ] **Step 1: Create `drawer-config.ts`**

```typescript
// frontend/projects/dsdevq-common/ui/src/lib/components/drawer/drawer-config.ts
import {InjectionToken} from '@angular/core';

export interface CmnDrawerOpenConfig<D = unknown> {
  data?: D;
  title?: string;
  width?: string;
  disableClose?: boolean;
}

export const CMN_DRAWER_DATA = new InjectionToken<unknown>('CmnDrawerData');
```

- [ ] **Step 2: Create `drawer-ref.ts`**

```typescript
// frontend/projects/dsdevq-common/ui/src/lib/components/drawer/drawer-ref.ts
import {type OverlayRef} from '@angular/cdk/overlay';
import {Injectable} from '@angular/core';
import {Subject, type Observable} from 'rxjs';

const CLOSE_ANIMATION_MS = 220;

@Injectable()
export class CmnDrawerRef<R = unknown> {
  private readonly beforeClose = new Subject<void>();
  private readonly closed = new Subject<R | undefined>();
  private isClosed = false;

  public readonly beforeClose$: Observable<void> = this.beforeClose.asObservable();

  public overlayRef!: OverlayRef;

  public close(result?: R): void {
    if (this.isClosed) { return; }
    this.isClosed = true;
    this.beforeClose.next();
    this.beforeClose.complete();
    setTimeout(() => {
      this.overlayRef.dispose();
      this.closed.next(result);
      this.closed.complete();
    }, CLOSE_ANIMATION_MS);
  }

  public afterClosed(): Observable<R | undefined> {
    return this.closed.asObservable();
  }
}
```

- [ ] **Step 3: Add drawer CSS to `theme.css`** (append after the dialog section)

```css
/* ─── Drawer (cmn-drawer) ───────────────────────────────────────────────────── */
.cmn-drawer-backdrop {
  background-color: rgb(0 0 0 / 0.35);
}

.cmn-drawer-panel {
  height: 100%;
}

cmn-drawer-container {
  display: flex;
  flex-direction: column;
  height: 100%;
  transform: translateX(100%);
  transition: transform 0.25s cubic-bezier(0.4, 0, 0.2, 1);
}

cmn-drawer-container.cmn-drawer--open {
  transform: translateX(0);
}

cmn-drawer-container.cmn-drawer--closing {
  transform: translateX(100%);
  transition: transform 0.22s cubic-bezier(0.4, 0, 0.2, 1);
}
```

- [ ] **Step 4: Lint-check the two new TS files**

```powershell
cd frontend
# Library files are in ignores:['projects/**'] so no eslint needed.
# Verify no TS errors by building later in Task 4.
```

---

## Task 2: CmnDrawerContainerComponent

**Files:**
- Create: `frontend/projects/dsdevq-common/ui/src/lib/components/drawer/drawer-container.component.ts`

- [ ] **Step 1: Write the component**

```typescript
// frontend/projects/dsdevq-common/ui/src/lib/components/drawer/drawer-container.component.ts
import {CdkPortalOutlet} from '@angular/cdk/portal';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  HostBinding,
  inject,
  signal,
} from '@angular/core';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';

import {IconComponent} from '../icon/icon.component';
import {CmnDrawerRef} from './drawer-ref';

type DrawerState = 'entering' | 'open' | 'closing';

@Component({
  selector: 'cmn-drawer-container',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CdkPortalOutlet, IconComponent],
  template: `
    <div
      class="flex h-full flex-col bg-surface-card"
      style="min-width: 0"
    >
      <!-- Header -->
      <div class="flex shrink-0 items-center justify-between border-b border-border-default px-cmn-6 py-cmn-4">
        <h2 class="text-cmn-base font-semibold text-text-primary">{{ title() }}</h2>
        <button
          (click)="drawerRef.close()"
          class="flex h-8 w-8 items-center justify-center rounded-cmn-md text-text-secondary transition-colors hover:bg-surface-raised hover:text-text-primary"
          aria-label="Close drawer"
        >
          <cmn-icon name="X" size="sm" />
        </button>
      </div>
      <!-- Body -->
      <div class="flex-1 overflow-y-auto">
        <ng-template cdkPortalOutlet />
      </div>
    </div>
  `,
})
export class CmnDrawerContainerComponent {
  public readonly title = signal('');
  public readonly drawerRef = inject(CmnDrawerRef);

  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private state: DrawerState = 'entering';

  @HostBinding('class.cmn-drawer--open')
  get isOpen(): boolean { return this.state === 'open'; }

  @HostBinding('class.cmn-drawer--closing')
  get isClosing(): boolean { return this.state === 'closing'; }

  public readonly portalOutlet = inject(CdkPortalOutlet, {self: true, optional: true})!;

  ngAfterViewInit(): void {
    requestAnimationFrame(() => {
      this.state = 'open';
      this.cdr.markForCheck();
    });

    this.drawerRef.beforeClose$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.state = 'closing';
        this.cdr.markForCheck();
      });
  }
}
```

> **Note:** `CdkPortalOutlet` injected via `{self: true}` won't work — the `portalOutlet` is accessed via `@ViewChild`. Fix in next step.

- [ ] **Step 2: Fix `portalOutlet` access — use `viewChild` signal query**

Replace the `portalOutlet` line and add a proper `viewChild`:

```typescript
import {viewChild} from '@angular/core';

// Inside class, remove the inject(CdkPortalOutlet) line and replace with:
public readonly portalOutlet = viewChild.required(CdkPortalOutlet);
```

Full corrected class body:

```typescript
export class CmnDrawerContainerComponent {
  public readonly title = signal('');
  public readonly drawerRef = inject(CmnDrawerRef);
  public readonly portalOutlet = viewChild.required(CdkPortalOutlet);

  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private state: DrawerState = 'entering';

  @HostBinding('class.cmn-drawer--open')
  get isOpen(): boolean { return this.state === 'open'; }

  @HostBinding('class.cmn-drawer--closing')
  get isClosing(): boolean { return this.state === 'closing'; }

  ngAfterViewInit(): void {
    requestAnimationFrame(() => {
      this.state = 'open';
      this.cdr.markForCheck();
    });

    this.drawerRef.beforeClose$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.state = 'closing';
        this.cdr.markForCheck();
      });
  }
}
```

---

## Task 3: CmnDrawerService

**Files:**
- Create: `frontend/projects/dsdevq-common/ui/src/lib/services/drawer/drawer.service.ts`

- [ ] **Step 1: Write the service**

```typescript
// frontend/projects/dsdevq-common/ui/src/lib/services/drawer/drawer.service.ts
import {Overlay} from '@angular/cdk/overlay';
import {ComponentPortal} from '@angular/cdk/portal';
import {type ComponentType} from '@angular/cdk/portal';
import {inject, Injectable, Injector} from '@angular/core';

import {CmnDrawerContainerComponent} from '../../components/drawer/drawer-container.component';
import {CMN_DRAWER_DATA, type CmnDrawerOpenConfig} from '../../components/drawer/drawer-config';
import {CmnDrawerRef} from '../../components/drawer/drawer-ref';

@Injectable({providedIn: 'root'})
export class CmnDrawerService {
  private readonly overlay = inject(Overlay);
  private readonly injector = inject(Injector);

  public open<R = unknown, D = unknown, C = unknown>(
    component: ComponentType<C>,
    config: CmnDrawerOpenConfig<D> = {}
  ): CmnDrawerRef<R> {
    const drawerRef = new CmnDrawerRef<R>();

    const overlayRef = this.overlay.create({
      width: config.width ?? '480px',
      height: '100%',
      positionStrategy: this.overlay.position().global().right().top(),
      hasBackdrop: true,
      backdropClass: 'cmn-drawer-backdrop',
      panelClass: 'cmn-drawer-panel',
      scrollStrategy: this.overlay.scrollStrategies.block(),
    });

    drawerRef.overlayRef = overlayRef;

    overlayRef.backdropClick().subscribe(() => {
      if (!config.disableClose) { drawerRef.close(); }
    });

    overlayRef.keydownEvents().subscribe(e => {
      if (e.key === 'Escape' && !config.disableClose) { drawerRef.close(); }
    });

    const childInjector = Injector.create({
      providers: [
        {provide: CmnDrawerRef, useValue: drawerRef},
        {provide: CMN_DRAWER_DATA, useValue: config.data ?? null},
      ],
      parent: this.injector,
    });

    const containerPortal = new ComponentPortal(CmnDrawerContainerComponent, null, childInjector);
    const containerRef = overlayRef.attach(containerPortal);

    containerRef.instance.title.set(config.title ?? '');

    const contentPortal = new ComponentPortal(component, null, childInjector);
    containerRef.instance.portalOutlet().attach(contentPortal);

    return drawerRef;
  }
}
```

---

## Task 4: Export from library index + build verification

**Files:**
- Modify: `frontend/projects/dsdevq-common/ui/src/lib/index.ts`

- [ ] **Step 1: Add exports**

After the existing dialog exports, add:

```typescript
export * from './components/drawer/drawer-config';
export * from './components/drawer/drawer-container.component';
export * from './components/drawer/drawer-ref';
export * from './services/drawer/drawer.service';
```

- [ ] **Step 2: Build the library to verify zero TS errors**

```powershell
cd frontend
npx ng build "@dsdevq-common/ui"
```

Expected: `Build at: ...` with no error lines. Fix any type errors before moving on.

---

## Task 5: CmnPasswordStrengthComponent

**Files:**
- Create: `frontend/projects/dsdevq-common/ui/src/lib/components/password-strength/password-strength.component.ts`
- Modify: `frontend/projects/dsdevq-common/ui/src/lib/index.ts`

> **Context:** The register template already has inline strength bars. This task extracts them into a library component so the rule "any new UI component in @dsdevq-common/ui first" is satisfied.

- [ ] **Step 1: Create the component**

```typescript
// frontend/projects/dsdevq-common/ui/src/lib/components/password-strength/password-strength.component.ts
import {ChangeDetectionStrategy, Component, computed, input} from '@angular/core';

const SCORE_COLORS: Record<number, string> = {
  1: 'bg-red-500',
  2: 'bg-amber-400',
  3: 'bg-blue-500',
  4: 'bg-green-500',
};

const SCORE_LABELS: Record<number, string> = {
  0: '',
  1: 'Weak',
  2: 'Fair',
  3: 'Good',
  4: 'Strong',
};

const SEGMENTS = [1, 2, 3, 4] as const;

@Component({
  selector: 'cmn-password-strength',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (score() > 0) {
      <div>
        <div class="mb-cmn-1 flex gap-1">
          @for (seg of segments; track seg) {
            <div
              class="h-1 flex-1 rounded-full transition-colors"
              [class]="seg <= score() ? activeColor() : 'bg-border-default'"
            ></div>
          }
        </div>
        <span class="text-cmn-xs text-text-secondary">{{ label() }}</span>
      </div>
    }
  `,
})
export class CmnPasswordStrengthComponent {
  public readonly score = input.required<number>();

  protected readonly segments = SEGMENTS;
  protected readonly activeColor = computed(() => SCORE_COLORS[this.score()] ?? 'bg-border-default');
  protected readonly label = computed(() => SCORE_LABELS[this.score()] ?? '');
}
```

- [ ] **Step 2: Export from index**

Add after alert export:

```typescript
export * from './components/password-strength/password-strength.component';
```

- [ ] **Step 3: Build library to verify**

```powershell
cd frontend
npx ng build "@dsdevq-common/ui"
```

Expected: build succeeds.

---

## Task 6: Wire `CmnPasswordStrengthComponent` into RegisterComponent

**Files:**
- Modify: `frontend/src/app/modules/auth/pages/register/register.component.ts`
- Modify: `frontend/src/app/modules/auth/pages/register/register.component.html`

- [ ] **Step 1: Add import to component**

In `register.component.ts`, add `CmnPasswordStrengthComponent` to imports array:

```typescript
import {
  AlertComponent,
  ButtonComponent,
  CmnPasswordStrengthComponent,
  FormFieldComponent,
  GoogleSignInButtonComponent,
  InputComponent,
} from '@dsdevq-common/ui';

// In @Component imports array:
imports: [
  ReactiveFormsModule,
  RouterLink,
  AlertComponent,
  ButtonComponent,
  CmnPasswordStrengthComponent,
  FormFieldComponent,
  GoogleSignInButtonComponent,
  InputComponent,
],
```

- [ ] **Step 2: Replace inline bars in the template**

In `register.component.html`, replace lines 79–101 (the `@if (passwordStrength() > 0)` block) with:

```html
<cmn-password-strength [score]="passwordStrength()" class="mt-cmn-2 block" />
```

- [ ] **Step 3: Lint and verify**

```powershell
cd frontend
npx eslint --fix src/app/modules/auth/pages/register/register.component.ts
npx eslint src/app/modules/auth/pages/register/register.component.ts
```

Expected: no errors.

---

## Task 7: Transaction Detail Drawer component

**Files:**
- Create: `frontend/src/app/modules/bank-sync/components/transaction-drawer/transaction-drawer.component.ts`

- [ ] **Step 1: Write the component**

The drawer displays all readable fields from `GlobalTransactionDto` (used by the ledger).

```typescript
// frontend/src/app/modules/bank-sync/components/transaction-drawer/transaction-drawer.component.ts
import {DatePipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {BadgeComponent, CMN_DRAWER_DATA, IconComponent} from '@dsdevq-common/ui';

import {MerchantCategoryPipe} from '../../../../shared/pipes/merchant-category.pipe';
import {TransactionAmountClassPipe} from '../../pipes/transaction-amount-class.pipe';
import {TransactionAmountPipe} from '../../pipes/transaction-amount.pipe';
import {type GlobalTransactionDto} from '../../models/transaction/transaction.model';

@Component({
  selector: 'fns-transaction-drawer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [BadgeComponent, DatePipe, IconComponent, MerchantCategoryPipe, TransactionAmountClassPipe, TransactionAmountPipe],
  template: `
    <div class="flex flex-col gap-cmn-6 p-cmn-6">
      <!-- Amount hero -->
      <div class="text-center">
        <p
          [class]="tx | transactionAmountClass"
          class="font-mono text-cmn-4xl font-bold tabular-nums"
        >{{ tx | transactionAmount }}</p>
        <p class="mt-cmn-1 text-cmn-sm text-text-secondary">
          {{ tx.postedDate ?? tx.date | date: 'MMMM d, y' }}
        </p>
      </div>

      <hr class="border-border-default" />

      <!-- Details grid -->
      <dl class="grid grid-cols-[auto_1fr] gap-x-cmn-4 gap-y-cmn-4 text-cmn-sm">
        <dt class="text-text-secondary">Description</dt>
        <dd class="font-medium text-text-primary">{{ tx.description }}</dd>

        <dt class="text-text-secondary">Account</dt>
        <dd class="text-text-primary">{{ tx.bankName }}</dd>

        <dt class="text-text-secondary">Status</dt>
        <dd>
          @if (tx.isPending) {
            <cmn-badge variant="warning">Pending</cmn-badge>
          } @else {
            <cmn-badge variant="success">Posted</cmn-badge>
          }
        </dd>

        @if (tx.merchantCategory) {
          <dt class="text-text-secondary">Category</dt>
          <dd>
            <cmn-badge variant="neutral">{{ tx.merchantCategory | merchantCategory }}</cmn-badge>
          </dd>
        }

        @if (tx.transactionType) {
          <dt class="text-text-secondary">Type</dt>
          <dd class="capitalize text-text-primary">{{ tx.transactionType }}</dd>
        }
      </dl>

      <hr class="border-border-default" />

      <!-- Notes (read-only placeholder) -->
      <div>
        <p class="mb-cmn-2 text-cmn-xs font-medium uppercase tracking-wider text-text-disabled">
          Notes
        </p>
        <div class="min-h-[80px] rounded-cmn-md border border-border-default bg-surface-bg px-cmn-3 py-cmn-2 text-cmn-sm text-text-disabled">
          No notes added
        </div>
      </div>
    </div>
  `,
})
export class TransactionDrawerComponent {
  public readonly tx = inject<GlobalTransactionDto>(CMN_DRAWER_DATA);
}
```

- [ ] **Step 2: Lint**

```powershell
cd frontend
npx eslint --fix src/app/modules/bank-sync/components/transaction-drawer/transaction-drawer.component.ts
npx eslint src/app/modules/bank-sync/components/transaction-drawer/transaction-drawer.component.ts
```

Expected: no errors.

---

## Task 8: Wire TxDrawer into TransactionLedgerComponent

**Files:**
- Modify: `frontend/src/app/modules/bank-sync/pages/transaction-ledger/transaction-ledger.component.ts`
- Modify: `frontend/src/app/modules/bank-sync/pages/transaction-ledger/transaction-ledger.component.html`

- [ ] **Step 1: Update the component class**

Replace the full content of `transaction-ledger.component.ts`:

```typescript
import {DatePipe, DecimalPipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {
  AlertComponent,
  BadgeComponent,
  ButtonComponent,
  CardComponent,
  CmnDrawerService,
  SkeletonComponent,
  StatCardComponent,
} from '@dsdevq-common/ui';

import {MerchantCategoryPipe} from '../../../../shared/pipes/merchant-category.pipe';
import {type GlobalTransactionDto} from '../../models/transaction/transaction.model';
import {TransactionAmountPipe} from '../../pipes/transaction-amount.pipe';
import {TransactionAmountClassPipe} from '../../pipes/transaction-amount-class.pipe';
import {TransactionDrawerComponent} from '../../components/transaction-drawer/transaction-drawer.component';
import {TransactionLedgerStore} from '../../store/transaction-ledger/transaction-ledger.store';

const SKELETON_ROWS = 8;

@Component({
  selector: 'fns-transaction-ledger',
  imports: [
    AlertComponent,
    BadgeComponent,
    ButtonComponent,
    CardComponent,
    DatePipe,
    DecimalPipe,
    MerchantCategoryPipe,
    SkeletonComponent,
    StatCardComponent,
    TransactionAmountClassPipe,
    TransactionAmountPipe,
  ],
  templateUrl: './transaction-ledger.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [TransactionLedgerStore],
})
export class TransactionLedgerComponent {
  private readonly drawer = inject(CmnDrawerService);

  public readonly store = inject(TransactionLedgerStore);
  public readonly skeletonRows = Array.from({length: SKELETON_ROWS});

  public openDrawer(tx: GlobalTransactionDto): void {
    this.drawer.open(TransactionDrawerComponent, {
      title: tx.description,
      data: tx,
      width: '480px',
    });
  }
}
```

- [ ] **Step 2: Add `(click)` to the table row in the template**

In `transaction-ledger.component.html`, find the `<tr>` that starts with:
```html
<tr
  class="border-b border-border-default last:border-0 hover:bg-surface-bg transition-colors"
>
```

Replace it with:
```html
<tr
  (click)="openDrawer(t)"
  class="cursor-pointer border-b border-border-default last:border-0 hover:bg-surface-bg transition-colors"
>
```

- [ ] **Step 3: Lint**

```powershell
cd frontend
npx eslint --fix src/app/modules/bank-sync/pages/transaction-ledger/transaction-ledger.component.ts
npx eslint src/app/modules/bank-sync/pages/transaction-ledger/transaction-ledger.component.ts
```

Expected: no errors.

---

## Task 9: Position model + PositionsService

**Files:**
- Create: `frontend/src/app/modules/holdings/models/position/position.model.ts`
- Create: `frontend/src/app/modules/holdings/services/positions.service.ts`

- [ ] **Step 1: Create `position.model.ts`**

```typescript
// frontend/src/app/modules/holdings/models/position/position.model.ts
export interface BrokeragePositionDto {
  symbol: string;
  instrumentType: string;
  quantity: number;
  usdValue: number;
}

export interface BrokerageHoldingsDto {
  provider: string;
  syncedAt: Nullable<string>;
  isStale: boolean;
  positions: BrokeragePositionDto[];
  totalUsdValue: number;
}

export interface CryptoHoldingDto {
  asset: string;
  freeQuantity: number;
  lockedQuantity: number;
  usdValue: number;
}

export interface CryptoHoldingsDto {
  provider: string;
  syncedAt: Nullable<string>;
  isStale: boolean;
  holdings: CryptoHoldingDto[];
  totalUsdValue: number;
}

export interface Position {
  symbol: string;
  provider: string;
  quantity: number;
  currentValue: number;
  currentPrice: number;
  mockPnlPercent: number;
}
```

- [ ] **Step 2: Create `positions.service.ts`**

```typescript
// frontend/src/app/modules/holdings/services/positions.service.ts
import {HttpClient} from '@angular/common/http';
import {inject, Injectable} from '@angular/core';
import {forkJoin, map, type Observable, of} from 'rxjs';
import {catchError} from 'rxjs/operators';

import {environment} from '../../../../environments/environment';
import {
  type BrokerageHoldingsDto,
  type CryptoHoldingsDto,
  type Position,
} from '../models/position/position.model';

function mockPnl(symbol: string): number {
  let hash = 0;
  for (const c of symbol) {
    hash = (hash * 31 + c.charCodeAt(0)) & 0xfffff;
  }
  return ((hash % 5000) - 2000) / 100;
}

@Injectable({providedIn: 'root'})
export class PositionsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  public getPositions(): Observable<Position[]> {
    const brokerage$ = this.http
      .get<BrokerageHoldingsDto>(`${this.baseUrl}/brokerage/holdings`)
      .pipe(catchError(() => of(null)));

    const crypto$ = this.http
      .get<CryptoHoldingsDto>(`${this.baseUrl}/crypto/holdings`)
      .pipe(catchError(() => of(null)));

    return forkJoin([brokerage$, crypto$]).pipe(
      map(([brokerage, crypto]) => {
        const brokeragePositions: Position[] = (brokerage?.positions ?? []).map(p => ({
          symbol: p.symbol,
          provider: brokerage?.provider ?? 'ibkr',
          quantity: p.quantity,
          currentValue: p.usdValue,
          currentPrice: p.quantity > 0 ? p.usdValue / p.quantity : 0,
          mockPnlPercent: mockPnl(p.symbol),
        }));

        const cryptoPositions: Position[] = (crypto?.holdings ?? []).map(h => ({
          symbol: h.asset,
          provider: crypto?.provider ?? 'binance',
          quantity: h.freeQuantity + h.lockedQuantity,
          currentValue: h.usdValue,
          currentPrice:
            h.freeQuantity + h.lockedQuantity > 0
              ? h.usdValue / (h.freeQuantity + h.lockedQuantity)
              : 0,
          mockPnlPercent: mockPnl(h.asset),
        }));

        return [...brokeragePositions, ...cryptoPositions];
      })
    );
  }
}
```

- [ ] **Step 3: Lint both files**

```powershell
cd frontend
npx eslint --fix src/app/modules/holdings/models/position/position.model.ts src/app/modules/holdings/services/positions.service.ts
npx eslint src/app/modules/holdings/models/position/position.model.ts src/app/modules/holdings/services/positions.service.ts
```

Expected: no errors.

---

## Task 10: Extend HoldingsStore for positions

**Files:**
- Modify: `frontend/src/app/modules/holdings/store/holdings.state.ts`
- Modify: `frontend/src/app/modules/holdings/store/holdings.methods.ts`
- Modify: `frontend/src/app/modules/holdings/store/holdings.computed.ts`
- Modify: `frontend/src/app/modules/holdings/store/holdings.effects.ts`

- [ ] **Step 1: Extend state**

Replace `holdings.state.ts` with:

```typescript
import {type Position} from '../models/position/position.model';
import {type WealthSummaryResponse} from '../../../shared/models/wealth/wealth.model';

export interface HoldingsState {
  summary: Nullable<WealthSummaryResponse>;
  status: AsyncStatus;
  errorCode: Nullable<string>;
  positions: Position[];
  positionsStatus: AsyncStatus;
  positionsErrorCode: Nullable<string>;
}

export const initialHoldingsState: HoldingsState = {
  summary: null,
  status: 'idle',
  errorCode: null,
  positions: [],
  positionsStatus: 'idle',
  positionsErrorCode: null,
};
```

- [ ] **Step 2: Add position mutations to methods**

Replace `holdings.methods.ts` with:

```typescript
import {patchState, type WritableStateSource} from '@ngrx/signals';

import {type Position} from '../models/position/position.model';
import {type WealthSummaryResponse} from '../../../shared/models/wealth/wealth.model';
import {type HoldingsState} from './holdings.state';

export function holdingsMethods(store: WritableStateSource<HoldingsState>) {
  return {
    setLoading(): void {
      patchState(store, {status: 'loading', errorCode: null});
    },
    setSummary(summary: WealthSummaryResponse): void {
      patchState(store, {summary, status: 'idle', errorCode: null});
    },
    setError(errorCode: Nullable<string>): void {
      patchState(store, {status: 'error', errorCode});
    },
    setPositionsLoading(): void {
      patchState(store, {positionsStatus: 'loading', positionsErrorCode: null});
    },
    setPositions(positions: Position[]): void {
      patchState(store, {positions, positionsStatus: 'idle', positionsErrorCode: null});
    },
    setPositionsError(errorCode: Nullable<string>): void {
      patchState(store, {positionsStatus: 'error', positionsErrorCode: errorCode});
    },
  };
}
```

- [ ] **Step 3: Add position computeds**

In `holdings.computed.ts`, add to the returned object:

```typescript
import {type Position} from '../models/position/position.model';

// Add after existing computeds:
positions: computed((): Position[] => store.positions()),
isPositionsLoading: computed(() => store.positionsStatus() === 'loading'),
positionsErrorMessage: computed(() => {
  if (store.positionsStatus() !== 'error') { return ''; }
  return errorMessages.resolve(store.positionsErrorCode()) ?? 'Failed to load positions.';
}),
positionsLoaded: computed(() => store.positionsStatus() === 'idle' && store.positions().length >= 0),
```

Full updated `holdings.computed.ts`:

```typescript
import {computed, inject, type Signal} from '@angular/core';
import {ErrorMessageService} from '@dsdevq-common/ui';

import {
  type AccountBalanceItem,
  type CategorySummary,
} from '../../../shared/models/wealth/wealth.model';
import {type Position} from '../models/position/position.model';
import {type HoldingsState} from './holdings.state';

interface StateSignals {
  summary: Signal<HoldingsState['summary']>;
  status: Signal<HoldingsState['status']>;
  errorCode: Signal<Nullable<string>>;
  positions: Signal<Position[]>;
  positionsStatus: Signal<HoldingsState['positionsStatus']>;
  positionsErrorCode: Signal<Nullable<string>>;
}

const DEFAULT_ERROR = 'Failed to load holdings. Please try again.';
const DEFAULT_POSITIONS_ERROR = 'Failed to load positions.';

export function holdingsComputed(store: StateSignals) {
  const errorMessages = inject(ErrorMessageService);

  return {
    isLoading: computed(() => store.status() === 'loading'),
    errorMessage: computed(() => {
      if (store.status() !== 'error') { return ''; }
      return errorMessages.resolve(store.errorCode()) ?? DEFAULT_ERROR;
    }),
    totalNetWorth: computed(() => store.summary()?.totalNetWorth ?? 0),
    baseCurrency: computed(() => store.summary()?.baseCurrency ?? 'USD'),
    bankingCategory: computed(
      (): Nullable<CategorySummary> =>
        store.summary()?.categories.find(c => c.category === 'banking') ?? null
    ),
    brokerageCategory: computed(
      (): Nullable<CategorySummary> =>
        store.summary()?.categories.find(c => c.category === 'brokerage') ?? null
    ),
    cryptoCategory: computed(
      (): Nullable<CategorySummary> =>
        store.summary()?.categories.find(c => c.category === 'crypto') ?? null
    ),
    allAccounts: computed(
      (): AccountBalanceItem[] => store.summary()?.categories.flatMap(c => c.accounts) ?? []
    ),
    positions: computed((): Position[] => store.positions()),
    isPositionsLoading: computed(() => store.positionsStatus() === 'loading'),
    positionsLoaded: computed(() => store.positionsStatus() !== 'idle' || store.positions().length > 0),
    positionsErrorMessage: computed(() => {
      if (store.positionsStatus() !== 'error') { return ''; }
      return errorMessages.resolve(store.positionsErrorCode()) ?? DEFAULT_POSITIONS_ERROR;
    }),
  };
}
```

- [ ] **Step 4: Add `loadPositions` effect**

Replace `holdings.effects.ts` with:

```typescript
import {inject} from '@angular/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {pipe, switchMap, tap} from 'rxjs';

import {StoreErrorUtils} from '../../../shared/utils/store-error.utils';
import {BinanceService} from '../../bank-sync/services/binance.service';
import {IBKRService} from '../../bank-sync/services/ibkr.service';
import {HoldingsService} from '../services/holdings.service';
import {PositionsService} from '../services/positions.service';
import {type HoldingsState} from './holdings.state';
import {type Position} from '../models/position/position.model';

interface StoreMethods {
  setLoading(): void;
  setSummary(summary: HoldingsState['summary'] & {}): void;
  setError(errorCode: Nullable<string>): void;
  setPositionsLoading(): void;
  setPositions(positions: Position[]): void;
  setPositionsError(errorCode: Nullable<string>): void;
}

export function holdingsEffects(store: StoreMethods) {
  const holdingsService = inject(HoldingsService);
  const binanceService = inject(BinanceService);
  const ibkrService = inject(IBKRService);
  const positionsService = inject(PositionsService);

  const load = rxMethod<void>(
    pipe(
      tap(() => store.setLoading()),
      switchMap(() =>
        holdingsService.getSummary().pipe(
          tap(summary => store.setSummary(summary)),
          StoreErrorUtils.catchAndSetError(store)
        )
      )
    )
  );

  const loadPositions = rxMethod<void>(
    pipe(
      tap(() => store.setPositionsLoading()),
      switchMap(() =>
        positionsService.getPositions().pipe(
          tap(positions => store.setPositions(positions)),
          StoreErrorUtils.catchAndSetError({
            setError: (code: Nullable<string>) => store.setPositionsError(code),
          })
        )
      )
    )
  );

  const disconnectBinance = rxMethod<void>(
    pipe(
      switchMap(() =>
        binanceService.disconnect().pipe(
          tap(() => load()),
          StoreErrorUtils.catchAndSetError(store)
        )
      )
    )
  );

  const disconnectIBKR = rxMethod<void>(
    pipe(
      switchMap(() =>
        ibkrService.disconnect().pipe(
          tap(() => load()),
          StoreErrorUtils.catchAndSetError(store)
        )
      )
    )
  );

  return {load, loadPositions, disconnectBinance, disconnectIBKR};
}

export function holdingsHooks(store: ReturnType<typeof holdingsEffects>) {
  return {
    onInit: () => store.load(),
  };
}
```

- [ ] **Step 5: Lint**

```powershell
cd frontend
npx eslint --fix src/app/modules/holdings/store/holdings.state.ts src/app/modules/holdings/store/holdings.methods.ts src/app/modules/holdings/store/holdings.computed.ts src/app/modules/holdings/store/holdings.effects.ts
npx eslint src/app/modules/holdings/store/holdings.state.ts src/app/modules/holdings/store/holdings.methods.ts src/app/modules/holdings/store/holdings.computed.ts src/app/modules/holdings/store/holdings.effects.ts
```

Expected: no errors.

---

## Task 11: Holdings Positions tab UI

**Files:**
- Modify: `frontend/src/app/modules/holdings/pages/holdings/holdings.component.ts`
- Modify: `frontend/src/app/modules/holdings/pages/holdings/holdings.component.html`

- [ ] **Step 1: Update component class**

Replace `holdings.component.ts` with:

```typescript
import {DecimalPipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject, signal, ViewContainerRef} from '@angular/core';
import {
  AlertComponent,
  BadgeComponent,
  ButtonComponent,
  CardComponent,
  CmnDialogService,
  StatCardComponent,
} from '@dsdevq-common/ui';
import {take} from 'rxjs';

import {type Provider} from '../../../../shared/models/provider/provider.model';
import {SyncStatusLabelPipe} from '../../../../shared/pipes/sync-status-label.pipe';
import {SyncStatusVariantPipe} from '../../../../shared/pipes/sync-status-variant.pipe';
import {DisconnectDialogComponent} from '../../../bank-sync/components/disconnect-dialog/disconnect-dialog.component';
import {CategoryLabelPipe} from '../../pipes/category-label.pipe';
import {CurrencyAmountPipe} from '../../pipes/currency-amount.pipe';
import {HoldingBalancePipe} from '../../pipes/holding-balance.pipe';
import {HoldingsStore} from '../../store/holdings.store';

const DISCONNECTABLE_PROVIDERS = new Set<Provider>(['binance', 'ibkr']);

export type HoldingsTab = 'holdings' | 'positions';

@Component({
  selector: 'fns-holdings',
  imports: [
    AlertComponent,
    BadgeComponent,
    ButtonComponent,
    CardComponent,
    CategoryLabelPipe,
    CurrencyAmountPipe,
    DecimalPipe,
    HoldingBalancePipe,
    StatCardComponent,
    SyncStatusLabelPipe,
    SyncStatusVariantPipe,
  ],
  templateUrl: './holdings.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [HoldingsStore],
})
export class HoldingsComponent {
  private readonly dialog = inject(CmnDialogService);
  private readonly viewContainerRef = inject(ViewContainerRef);

  public readonly store = inject(HoldingsStore);
  public readonly activeTab = signal<HoldingsTab>('holdings');

  public canDisconnect(provider: string): boolean {
    return DISCONNECTABLE_PROVIDERS.has(provider as Provider);
  }

  public switchTab(tab: HoldingsTab): void {
    this.activeTab.set(tab);
    if (tab === 'positions' && !this.store.positionsLoaded()) {
      this.store.loadPositions();
    }
  }

  public disconnect(provider: string, displayName: string): void {
    if (!this.canDisconnect(provider)) { return; }
    const ref = this.dialog.open<boolean>(DisconnectDialogComponent, {
      title: `Disconnect ${displayName}`,
      size: 'sm',
      viewContainerRef: this.viewContainerRef,
      data: {providerName: displayName},
    });
    ref
      .afterClosed()
      .pipe(take(1))
      .subscribe(confirmed => {
        if (confirmed !== true) { return; }
        if (provider === 'binance') {
          this.store.disconnectBinance();
        } else if (provider === 'ibkr') {
          this.store.disconnectIBKR();
        }
      });
  }
}
```

- [ ] **Step 2: Add tab bar and Positions table to the template**

In `holdings.component.html`, after the header `<div class="flex items-center justify-between">` block and before the error state `@if (store.errorMessage())`, insert the tab bar:

```html
<!-- Tab bar -->
<div class="flex gap-1 border-b border-border-default">
  <button
    (click)="switchTab('holdings')"
    class="px-cmn-4 py-cmn-2 text-cmn-sm font-medium transition-colors"
    [class.text-accent-default]="activeTab() === 'holdings'"
    [class.border-b-2]="activeTab() === 'holdings'"
    [class.border-accent-default]="activeTab() === 'holdings'"
    [class.text-text-secondary]="activeTab() !== 'holdings'"
  >Holdings</button>
  <button
    (click)="switchTab('positions')"
    class="px-cmn-4 py-cmn-2 text-cmn-sm font-medium transition-colors"
    [class.text-accent-default]="activeTab() === 'positions'"
    [class.border-b-2]="activeTab() === 'positions'"
    [class.border-accent-default]="activeTab() === 'positions'"
    [class.text-text-secondary]="activeTab() !== 'positions'"
  >Positions</button>
</div>
```

Then wrap the existing content (error alert through detailed holdings `cmn-card`) inside:

```html
@if (activeTab() === 'holdings') {
  <!-- existing content here unchanged -->
}
```

After that block, add the Positions tab:

```html
@if (activeTab() === 'positions') {
  @if (store.positionsErrorMessage()) {
    <cmn-alert variant="error">{{ store.positionsErrorMessage() }}</cmn-alert>
  }

  @if (store.isPositionsLoading()) {
    <cmn-card>
      <div class="animate-pulse space-y-3 p-cmn-4">
        @for (_ of [1,2,3,4,5]; track $index) {
          <div class="h-4 rounded bg-neutral-100 dark:bg-neutral-800"></div>
        }
      </div>
    </cmn-card>
  }

  @if (!store.isPositionsLoading()) {
    <cmn-card>
      @if (store.positions().length === 0) {
        <div class="py-cmn-12 text-center text-cmn-sm text-text-secondary">
          No positions found. Connect Binance or IBKR to see your positions.
        </div>
      } @else {
        <table class="w-full text-cmn-sm" aria-label="Positions">
          <thead>
            <tr class="border-b border-border-default text-cmn-xs text-text-secondary uppercase tracking-wide">
              <th class="py-cmn-3 px-cmn-4 text-left font-medium" scope="col">Symbol</th>
              <th class="py-cmn-3 px-cmn-4 text-left font-medium" scope="col">Provider</th>
              <th class="py-cmn-3 px-cmn-4 text-right font-medium" scope="col">Qty</th>
              <th class="py-cmn-3 px-cmn-4 text-right font-medium" scope="col">Price</th>
              <th class="py-cmn-3 px-cmn-4 text-right font-medium" scope="col">Value (USD)</th>
              <th class="py-cmn-3 px-cmn-4 text-right font-medium" scope="col">P&amp;L %</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-border-default">
            @for (p of store.positions(); track p.symbol + p.provider) {
              <tr class="hover:bg-surface-bg transition-colors">
                <td class="py-cmn-3 px-cmn-4 font-mono font-semibold text-text-primary">
                  {{ p.symbol }}
                </td>
                <td class="py-cmn-3 px-cmn-4 uppercase text-cmn-xs text-text-secondary">
                  {{ p.provider }}
                </td>
                <td class="py-cmn-3 px-cmn-4 text-right font-mono tabular-nums text-text-primary">
                  {{ p.quantity | number: '1.0-6' }}
                </td>
                <td class="py-cmn-3 px-cmn-4 text-right font-mono tabular-nums text-text-secondary">
                  ${{ p.currentPrice | number: '1.2-2' }}
                </td>
                <td class="py-cmn-3 px-cmn-4 text-right font-mono tabular-nums font-medium text-text-primary">
                  ${{ p.currentValue | number: '1.2-2' }}
                </td>
                <td
                  class="py-cmn-3 px-cmn-4 text-right font-mono tabular-nums font-medium"
                  [class.text-green-600]="p.mockPnlPercent >= 0"
                  [class.text-red-500]="p.mockPnlPercent < 0"
                >
                  {{ p.mockPnlPercent >= 0 ? '+' : '' }}{{ p.mockPnlPercent | number: '1.2-2' }}%
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
    </cmn-card>
  }
}
```

- [ ] **Step 3: Lint**

```powershell
cd frontend
npx eslint --fix src/app/modules/holdings/pages/holdings/holdings.component.ts
npx eslint src/app/modules/holdings/pages/holdings/holdings.component.ts
```

Expected: no errors.

---

## Task 12: TypeScript verification + commit

- [ ] **Step 1: Full type-check**

```powershell
cd frontend
npx tsc --noEmit -p tsconfig.app.json
```

Expected: no output (zero errors).

- [ ] **Step 2: Rebuild the library**

```powershell
cd frontend
npx ng build "@dsdevq-common/ui"
```

Expected: `Build at: ...` with no errors.

- [ ] **Step 3: Commit**

```powershell
git add `
  frontend/projects/dsdevq-common/ui/src/lib/components/drawer/ `
  frontend/projects/dsdevq-common/ui/src/lib/services/drawer/ `
  frontend/projects/dsdevq-common/ui/src/lib/components/password-strength/ `
  frontend/projects/dsdevq-common/ui/src/lib/index.ts `
  frontend/projects/dsdevq-common/ui/src/styles/theme.css `
  frontend/src/app/modules/bank-sync/components/transaction-drawer/ `
  frontend/src/app/modules/bank-sync/pages/transaction-ledger/ `
  frontend/src/app/modules/holdings/ `
  frontend/src/app/modules/auth/pages/register/

git commit -m "$(cat <<'EOF'
feat: drawer service, transaction detail drawer, holdings positions tab, password strength component

- CmnDrawerService: CDK Overlay-based right-side slide-in drawer with backdrop, ESC close, and CSS animation
- TransactionDrawerComponent: read-only drawer for transaction details (amount hero, category, date, status, notes placeholder)
- Holdings Positions tab: lazy-loads brokerage + crypto positions with symbol/qty/price/value; mock P&L%
- CmnPasswordStrengthComponent: extracted from inline register template into @dsdevq-common/ui

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
EOF
)"
```

---

## Self-Review

### Spec coverage check
- ✅ DrawerService with CDK Overlay, backdrop, ESC, slide animation — Tasks 1–4
- ✅ TxDrawer: amount hero, description, date, status badge, category chip, notes placeholder — Task 7
- ✅ TxDrawer opened on row click from ledger — Task 8
- ✅ Holdings Positions tab: symbol, qty, current price, current value, mock P&L% — Tasks 9–11
- ✅ Password strength extracted to `CmnPasswordStrengthComponent` — Tasks 5–6

### Potential issues to watch
1. **Task 3 — `portalOutlet().attach()` timing**: `containerRef.instance.portalOutlet()` is a signal query; by the time the service calls it, `ngAfterViewInit` may not have run yet. If attach fails, wrap in `containerRef.changeDetectorRef.detectChanges()` before calling `portalOutlet().attach(contentPortal)`.
2. **Task 10 — `StoreErrorUtils.catchAndSetError` signature**: the `loadPositions` effect passes `{setError: ...}` — verify `StoreErrorUtils` accepts any object with a `setError` method; if it requires the full `StoreMethods` shape, add a dummy no-op `setLoading`.
3. **Task 11 — template lint**: attribute order on `[class.*]` bindings may require `--fix` pass.
