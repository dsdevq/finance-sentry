import {ChangeDetectionStrategy, Component, computed, inject} from '@angular/core';
import {BadgeComponent, SelectableCardComponent} from '@dsdevq-common/ui';

import {type Provider} from '../../../../shared/models/provider/provider.model';
import {type InstitutionType} from '../../../../shared/models/provider/provider.model';
import {ConnectStore} from '../../store/connect/connect.store';

interface TypeTile {
  readonly id: InstitutionType;
  readonly label: string;
  readonly description: string;
  readonly badge: string;
}

const TILES: readonly TypeTile[] = [
  {id: 'bank', label: 'Bank', description: 'Plaid · Monobank', badge: 'bank'},
  {id: 'crypto', label: 'Crypto', description: 'Binance', badge: 'binance'},
  {id: 'broker', label: 'Brokerage', description: 'Interactive Brokers', badge: 'ibkr'},
];

const PROVIDERS_FOR_TYPE: Record<InstitutionType, readonly Provider[]> = {
  bank: ['plaid', 'monobank'],
  crypto: ['binance'],
  broker: ['ibkr'],
};

@Component({
  selector: 'fns-type-picker',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [BadgeComponent, SelectableCardComponent],
  templateUrl: './type-picker.component.html',
})
export class TypePickerComponent {
  public readonly store = inject(ConnectStore);

  public readonly tiles = TILES;

  public readonly connectionsByType = computed<Record<InstitutionType, number>>(() => {
    const connected = this.store.connectedProviders();
    return {
      bank: PROVIDERS_FOR_TYPE.bank.filter(s => connected.has(s)).length,
      crypto: PROVIDERS_FOR_TYPE.crypto.filter(s => connected.has(s)).length,
      broker: PROVIDERS_FOR_TYPE.broker.filter(s => connected.has(s)).length,
    };
  });

  public select(id: InstitutionType): void {
    this.store.selectInstitutionType(id);
  }
}
