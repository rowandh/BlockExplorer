import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { StoreModule } from '@ngrx/store';
import { EffectsModule } from '@ngrx/effects';
import {
  TRANSACTIONS_FEATURE_KEY,
  initialState as transactionsInitialState,
  transactionsReducer
} from './+state/transactions.reducer';
import { TransactionsEffects } from './+state/transactions.effects';
import { TransactionsFacade } from './+state/transactions.facade';

@NgModule({
  imports: [
    CommonModule,
    StoreModule.forFeature(TRANSACTIONS_FEATURE_KEY, transactionsReducer, {
      initialState: transactionsInitialState
    }),
    EffectsModule.forFeature([TransactionsEffects])
  ],
  providers: [TransactionsFacade]
})
export class StateTransactionsStateModule {}
