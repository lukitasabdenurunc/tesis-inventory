import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { StockService } from '../../../services/stock.service';
import { RubroService } from '../../../services/rubro.service';
import { FamiliaService } from '../../../services/familia.service';
import { SedeService } from '../../../services/sede.service';
import { SedeContextService } from '../../../services/sede-context.service';
import { Subscription } from 'rxjs';
import { IncrementarStockDto, RegistrarConsumoDto, RegistrarTransferenciaDto, Stock } from '../../../models/stock.model';
import { Rubro } from '../../../models/rubro.model';
import { Familia } from '../../../models/familia.model';

@Component({
  selector: 'app-stock',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './stock.html',
  styleUrls: ['./stock.css']
})
export class StockComponent implements OnInit {
  stocks: Stock[] = [];
  rubros: Rubro[] = [];
  familias: Familia[] = [];
  sedes: any[] = [];
  
  private contextSub!: Subscription;

  // Paginación
  totalCount: number = 0;
  page: number = 1;
  pageSize: number = 50;
  totalPages: number = 1;

  // Filtros
  searchTerm: string = '';
  selectedRubroId: number | null = null;
  selectedFamiliaId: number | null = null;
  selectedEstado: string = '';
  selectedBajoStock: string = '';
  hasSearched: boolean = false;

  // Modals state
  activeModal: 'consumo' | 'transferencia' | 'incremento' | 'historial' | 'detalle' | null = null;
  selectedStock: Stock | null = null;

  // Consumo state
  consumoCantidad: number = 1;
  consumoMotivo: number = 0; // 0=Consumo, 1=Vencimiento, 2=Daño
  consumoConfirm: boolean = false;

  // Incremento state
  incrementoCantidad: number = 1;
  incrementoMotivo: number = 3; // 3=Compra, 4=Ajustes
  incrementoObs: string = '';

  // Transferencia state
  transferenciaSedeDestino: number | null = null;
  transferenciaCantidad: number = 1;
  transferenciaObs: string = '';

  // Historial state
  historial: any[] = [];
  histFiltroTipo: string = '';
  histFiltroDesde: string = '';
  histFiltroHasta: string = '';

  constructor(
    private stockService: StockService,
    private rubroService: RubroService,
    private familiaService: FamiliaService,
    private sedeService: SedeService,
    private sedeContextService: SedeContextService
  ) { }

  ngOnInit(): void {
    this.loadRubros();
    this.loadSedes();
    // Suscribirse a cambios de sede para recargar
    this.contextSub = this.sedeContextService.sedeEnContexto$.subscribe(() => {
      this.hasSearched = false;
      this.page = 1;
      this.loadStocks();
    });
  }

  ngOnDestroy(): void {
    if (this.contextSub) this.contextSub.unsubscribe();
  }

  loadRubros(): void {
    this.rubroService.getAll(true).subscribe(data => this.rubros = data);
  }

  loadSedes(): void {
    this.sedeService.getAll().subscribe(data => this.sedes = data);
  }

  onRubroChange(): void {
    this.selectedFamiliaId = null;
    this.familias = [];
    if (this.selectedRubroId) {
      this.familiaService.getByRubro(this.selectedRubroId, true).subscribe(data => this.familias = data);
    }
    this.search();
  }

  loadStocks(): void {
    const estadoBool = this.selectedEstado === '' ? undefined : (this.selectedEstado === 'true');
    const bajoStockBool = this.selectedBajoStock === '' ? undefined : (this.selectedBajoStock === 'true');

    this.stockService.getStockSede(
      this.searchTerm || undefined,
      this.selectedRubroId || undefined,
      this.selectedFamiliaId || undefined,
      estadoBool,
      bajoStockBool,
      this.page,
      this.pageSize
    ).subscribe(res => {
      this.stocks = res.data;
      this.totalCount = res.totalCount;
      this.page = res.page;
      this.totalPages = res.totalPages;
    });
  }

  search(): void {
    this.hasSearched = true;
    this.page = 1;
    this.loadStocks();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedRubroId = null;
    this.selectedFamiliaId = null;
    this.selectedEstado = '';
    this.selectedBajoStock = '';
    this.familias = [];
    this.hasSearched = false;
    this.page = 1;
    this.loadStocks();
  }

  nextPage(): void {
    if (this.page < this.totalPages) {
      this.page++;
      this.loadStocks();
    }
  }

  prevPage(): void {
    if (this.page > 1) {
      this.page--;
      this.loadStocks();
    }
  }

  // Modals hooks
  closeModal(): void {
    this.activeModal = null;
    this.selectedStock = null;
    this.consumoConfirm = false;
  }

  openConsumoModal(stock: Stock): void {
    this.selectedStock = stock;
    this.consumoCantidad = 1;
    this.consumoMotivo = 0;
    this.consumoConfirm = false;
    this.activeModal = 'consumo';
  }

  confirmConsumo(): void {
    if (!this.selectedStock) return;

    if (!this.consumoConfirm) {
      this.consumoConfirm = true;
      return;
    }

    const dto: RegistrarConsumoDto = {
      idProducto: this.selectedStock.idProducto,
      cantidad: this.consumoCantidad,
      motivo: this.consumoMotivo
    };

    this.stockService.registrarConsumo(dto).subscribe({
      next: () => {
        this.closeModal();
        this.loadStocks();
      },
      error: (err) => alert(err.error?.message || 'Error al registrar consumo')
    });
  }

  openTransferenciaModal(stock: Stock): void {
    this.selectedStock = stock;
    this.transferenciaSedeDestino = null;
    this.transferenciaCantidad = 1;
    this.transferenciaObs = '';
    this.activeModal = 'transferencia';
  }

  saveTransferencia(): void {
    if (!this.selectedStock || !this.transferenciaSedeDestino) return;

    const dto: RegistrarTransferenciaDto = {
      idProducto: this.selectedStock.idProducto,
      idSedeDestino: this.transferenciaSedeDestino,
      cantidad: this.transferenciaCantidad,
      observaciones: this.transferenciaObs
    };

    this.stockService.registrarTransferencia(dto).subscribe({
      next: () => {
        this.closeModal();
        this.loadStocks();
      },
      error: (err) => alert(err.error?.message || 'Error al registrar transferencia')
    });
  }

  openIncrementoModal(stock: Stock): void {
    this.selectedStock = stock;
    this.incrementoCantidad = 1;
    this.incrementoMotivo = 3;
    this.incrementoObs = '';
    this.activeModal = 'incremento';
  }

  saveIncremento(): void {
    if (!this.selectedStock) return;

    const dto: IncrementarStockDto = {
      idProducto: this.selectedStock.idProducto,
      cantidad: this.incrementoCantidad,
      motivo: this.incrementoMotivo,
      observaciones: this.incrementoObs
    };

    this.stockService.incrementarStock(dto).subscribe({
      next: () => {
        this.closeModal();
        this.loadStocks();
      },
      error: (err) => alert(err.error?.message || 'Error al incrementar stock')
    });
  }

  openHistorialModal(stock: Stock): void {
    this.selectedStock = stock;
    this.activeModal = 'historial';
    this.histFiltroTipo = '';
    this.histFiltroDesde = '';
    this.histFiltroHasta = '';
    this.loadHistorial();
  }

  loadHistorial(): void {
    if (!this.selectedStock) return;
    this.stockService.getMovimientos(this.selectedStock.idProducto, this.histFiltroTipo || undefined, this.histFiltroDesde || undefined, this.histFiltroHasta || undefined)
      .subscribe({
        next: (data) => this.historial = data,
        error: (err) => alert('Error al cargar historial')
      });
  }

  openDetalleModal(stock: Stock): void {
    this.selectedStock = stock;
    this.activeModal = 'detalle';
    // Lógica de atributos puede ir aquí
  }
}
