import { Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, map, switchMap } from 'rxjs/operators';
import { of } from 'rxjs';

import * as StandardTokensActions from '../actions/standard-tokens.actions';
import { SmartContractService } from '../../services/smartcontract.service';


@Injectable()
export class StandardTokensEffects {

    constructor(private actions$: Actions, private smartContractService: SmartContractService) { }

    loadTokens$ = createEffect(() =>
        this.actions$.pipe(
            ofType(StandardTokensActions.loadStandardTokens),
            switchMap(action => {
                return this.smartContractService.getStandardTokens(action.from, action.records).pipe(
                    map(standardTokens => StandardTokensActions.standardTokensLoaded({ entities: standardTokens })),
                    catchError(error => of(StandardTokensActions.standardTokensLoadError({ error: error.toString() })))
                );
            })
        )
    );

    loadToken$ = createEffect(() =>
        this.actions$.pipe(
            ofType(StandardTokensActions.loadStandardToken),
            switchMap(action => {
                return this.smartContractService.getStandardToken(action.id.toString()).pipe(
                    map(standardToken => StandardTokensActions.standardTokenLoaded({ entity: standardToken })),
                    catchError(error => of(StandardTokensActions.standardTokenLoadError({ error: error.toString() })))
                );
            })
        )
    );
}