import {DatePipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {CMN_DRAWER_DATA, TagComponent} from '@dsdevq-common/ui';

import {MerchantCategoryPipe} from '../../../../shared/pipes/merchant-category.pipe';
import {type GlobalTransactionDto} from '../../models/transaction/transaction.model';
import {TransactionAmountPipe} from '../../pipes/transaction-amount.pipe';
import {TransactionAmountClassPipe} from '../../pipes/transaction-amount-class.pipe';

@Component({
  selector: 'fns-transaction-drawer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    TagComponent,
    DatePipe,
    MerchantCategoryPipe,
    TransactionAmountClassPipe,
    TransactionAmountPipe,
  ],
  template: `
    <div class="flex flex-col gap-cmn-6 p-cmn-6">
      <!-- Amount hero -->
      <div class="text-center">
        <p
          [class]="tx | transactionAmountClass"
          class="font-mono text-cmn-4xl font-bold tabular-nums"
        >
          {{ tx | transactionAmount }}
        </p>
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
            <cmn-tag variant="warning">Pending</cmn-tag>
          } @else {
            <cmn-tag variant="success">Posted</cmn-tag>
          }
        </dd>

        @if (tx.merchantCategory) {
          <dt class="text-text-secondary">Category</dt>
          <dd>
            <cmn-tag variant="neutral">{{ tx.merchantCategory | merchantCategory }}</cmn-tag>
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
        <div
          class="min-h-[80px] rounded-cmn-md border border-border-default bg-surface-bg px-cmn-3 py-cmn-2 text-cmn-sm text-text-disabled"
        >
          No notes added
        </div>
      </div>
    </div>
  `,
})
export class TransactionDrawerComponent {
  public readonly tx = inject<GlobalTransactionDto>(CMN_DRAWER_DATA);
}
