import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { IncrementarStockDto, Movimiento, RegistrarConsumoDto, RegistrarTransferenciaDto, Stock } from '../models/stock.model';

@Injectable({
    providedIn: 'root'
})
export class StockService {
    private apiUrl = 'http://localhost:5139/api/Stock';

    constructor(private http: HttpClient) { }


    getStockSede(search?: string, idRubro?: number, idFamilia?: number, estado?: boolean, bajoStock?: boolean, page: number = 1, pageSize: number = 50, idSedeQuery?: number): Observable<any> {
        let params = new HttpParams()
            .set('page', page.toString())
            .set('pageSize', pageSize.toString());

        if (search) params = params.set('search', search);
        if (idRubro !== undefined && idRubro !== null) params = params.set('idRubro', idRubro.toString());
        if (idFamilia !== undefined && idFamilia !== null) params = params.set('idFamilia', idFamilia.toString());
        if (estado !== undefined && estado !== null) params = params.set('estado', estado.toString());
        if (bajoStock !== undefined && bajoStock !== null) params = params.set('bajoStock', bajoStock.toString());
        if (idSedeQuery !== undefined && idSedeQuery !== null) params = params.set('idSedeQuery', idSedeQuery.toString());

        return this.http.get<any>(`${this.apiUrl}/sede`, { params });
    }

    incrementarStock(dto: IncrementarStockDto): Observable<any> {
        return this.http.post<any>(`${this.apiUrl}/incremento`, dto);
    }

    registrarConsumo(dto: RegistrarConsumoDto): Observable<any> {
        return this.http.post<any>(`${this.apiUrl}/consumo`, dto);
    }

    registrarTransferencia(dto: RegistrarTransferenciaDto): Observable<any> {
        return this.http.post<any>(`${this.apiUrl}/transferencia`, dto);
    }

    getMovimientos(idProducto: number, tipoMovimiento?: string, fechaDesde?: string, fechaHasta?: string): Observable<Movimiento[]> {
        let params = new HttpParams();
        if (tipoMovimiento) params = params.set('tipoMovimiento', tipoMovimiento);
        if (fechaDesde) params = params.set('fechaDesde', fechaDesde);
        if (fechaHasta) params = params.set('fechaHasta', fechaHasta);

        return this.http.get<Movimiento[]>(`${this.apiUrl}/${idProducto}/movimientos`, { params });
    }

    getHistorialGlobal(search?: string, idRubro?: number, idFamilia?: number, tipoMovimiento?: string, idUsuario?: number, fechaDesde?: string, fechaHasta?: string, page: number = 1, pageSize: number = 50): Observable<any> {
        let params = new HttpParams()
            .set('page', page.toString())
            .set('pageSize', pageSize.toString());

        if (search) params = params.set('search', search);
        if (idRubro !== undefined && idRubro !== null) params = params.set('idRubro', idRubro.toString());
        if (idFamilia !== undefined && idFamilia !== null) params = params.set('idFamilia', idFamilia.toString());
        if (tipoMovimiento) params = params.set('tipoMovimiento', tipoMovimiento);
        if (idUsuario !== undefined && idUsuario !== null) params = params.set('idUsuario', idUsuario.toString());
        if (fechaDesde) params = params.set('fechaDesde', fechaDesde);
        if (fechaHasta) params = params.set('fechaHasta', fechaHasta);

        return this.http.get<any>(`${this.apiUrl}/movimientos/historial`, { params });
    }
}
