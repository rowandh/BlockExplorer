import { Injectable } from '@angular/core';
import { select, Store } from '@ngrx/store';
import * as fromActions from './actions';
import * as fromReducers from './reducers';
import * as fromSelectors from './selectors';

@Injectable({ providedIn: 'root' })
export class BlockStoreFacade {
    areBlocksLoaded$ = this.store.pipe(select(fromSelectors.getAreBlocksLoaded));
    isSelectedBlockLoaded$ = this.store.pipe(select(fromSelectors.getIsSelectedBlockLoaded));

    selectedBlockError$ = this.store.pipe(select(fromSelectors.getSelectedBlockError$));
    selectedBlock$ = this.store.pipe(select(fromSelectors.getSelectedBlock));

    areLastBlocksLoaded$ = this.store.pipe(select(fromSelectors.getAreLastBlocksLoaded));
    lastBlocks$ = this.store.pipe(select(fromSelectors.getLastBlocks));

    constructor(private store: Store<fromReducers.BlockState>) { }


    loadBlock(blockHash: string) {
        this.store.dispatch(fromActions.loadBlock(blockHash));
    }

    loadLastBlocks(records: number) {
        this.store.dispatch(fromActions.loadLastBlocks(records));
    }
}