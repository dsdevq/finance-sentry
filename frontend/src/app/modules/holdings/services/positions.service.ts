import {Injectable} from '@angular/core';
import {ApiService} from '@dsdevq-common/core';
import {forkJoin, map, type Observable, of} from 'rxjs';
import {catchError} from 'rxjs/operators';

import {
  type BrokerageHoldingsDto,
  type CryptoHoldingsDto,
  type Position,
} from '../models/position/position.model';

const HASH_MULTIPLIER = 31;
const HASH_MASK = 0xfffff;
const PNL_RANGE = 5000;
const PNL_OFFSET = 2000;
const PNL_SCALE = 100;

function mockPnl(symbol: string): number {
  let hash = 0;
  for (const c of symbol) {
    hash = (hash * HASH_MULTIPLIER + c.charCodeAt(0)) & HASH_MASK;
  }
  return ((hash % PNL_RANGE) - PNL_OFFSET) / PNL_SCALE;
}

@Injectable({providedIn: 'root'})
export class PositionsService extends ApiService {
  constructor() {
    super('');
  }

  public getPositions(): Observable<Position[]> {
    const brokerage$ = this.get<BrokerageHoldingsDto>('brokerage/holdings').pipe(
      catchError(() => of(null))
    );

    const crypto$ = this.get<CryptoHoldingsDto>('crypto/holdings').pipe(catchError(() => of(null)));

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
