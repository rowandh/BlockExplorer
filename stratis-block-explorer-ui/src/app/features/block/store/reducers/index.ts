import { ActionReducerMap,  Action } from '@ngrx/store';
import * as fromBlocks from './blocks.reducer';
import * as fromLastBlock from './last-block.reducer';
import * as fromSelectedBlock from './selected-block.reducer';

export const blockFeatureKey = 'block';

export interface BlockState {
    blocks: fromBlocks.BlocksState,
    lastBlock: fromLastBlock.LastBlocksState
    selectedBlock: fromSelectedBlock.SelectedBlockState
}

export const reducers: ActionReducerMap<BlockState> = {
    blocks: fromBlocks.reducer,
    lastBlock: fromLastBlock.reducer,
    selectedBlock: fromSelectedBlock.reducer
};

//export function reducers(state: BlockState | undefined, action: Action) {
//    return blockReducers(state, action);
//}
