import {NgOptimizedImage} from '@angular/common';
import {ChangeDetectionStrategy, Component, computed, inject} from '@angular/core';
import {
  ButtonComponent,
  DialogActionsComponent,
  SelectableCardComponent,
  TagComponent,
} from '@dsdevq-common/ui';

import {PROVIDER_CATALOG} from '../../../../shared/constants/providers/providers.constants';
import {
  type BankProvider,
  type ProviderDescriptor,
} from '../../../../shared/models/provider/provider.model';
import {ConnectStore} from '../../store/connect/connect.store';

const BANK_PROVIDERS: readonly ProviderDescriptor[] = PROVIDER_CATALOG.filter(
  p => p.institutionType === 'bank'
);

@Component({
  selector: 'fns-bank-picker',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    TagComponent,
    ButtonComponent,
    DialogActionsComponent,
    NgOptimizedImage,
    SelectableCardComponent,
  ],
  templateUrl: './bank-picker.component.html',
})
export class BankPickerComponent {
  public readonly store = inject(ConnectStore);

  public readonly providers = BANK_PROVIDERS;

  public readonly connected = computed(() => this.store.connectedProviders());

  public select(slug: string): void {
    this.store.selectBankProvider(slug as BankProvider);
  }

  public back(): void {
    this.store.setModalStep('type-picker');
  }
}
