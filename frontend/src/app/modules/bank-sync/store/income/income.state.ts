import {type MonthlyFlow} from '../../models/dashboard/dashboard.model';
import {type GlobalTransactionDto} from '../../models/transaction/transaction.model';

export interface IncomeState {
  monthlyFlow: MonthlyFlow[];
  transactions: GlobalTransactionDto[];
  totalCount: number;
  hasMore: boolean;
  offset: number;
}

export const INCOME_PAGE_SIZE = 50;

export const initialIncomeState: IncomeState = {
  monthlyFlow: [],
  transactions: [],
  totalCount: 0,
  hasMore: false,
  offset: 0,
};
