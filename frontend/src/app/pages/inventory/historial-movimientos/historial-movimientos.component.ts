import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StockService } from '../../../services/stock.service';
import { RubroService } from '../../../services/rubro.service';
import { FamiliaService } from '../../../services/familia.service';
import { UserService } from '../../../services/user.service';
import { SedeContextService } from '../../../services/sede-context.service';
import { Movimiento } from '../../../models/stock.model';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-historial-movimientos',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './historial-movimientos.component.html',
  styleUrls: ['./historial-movimientos.component.css']
})
export class HistorialMovimientosComponent implements OnInit {

  private contextSub!: Subscription;
  movimientos: Movimiento[] = [];
  
  // Filters
  searchTerm: string = '';
  selectedRubroId: number | null = null;
  selectedFamiliaId: number | null = null;
  selectedTipoMovimiento: string = '';
  selectedUsuarioId: number | null = null;
  fechaDesde: string = '';
  fechaHasta: string = '';

  // Pagination
  page: number = 1;
  pageSize: number = 50;
  totalCount: number = 0;
  totalPages: number = 1;

  // Selectors Data
  rubros: any[] = [];
  familias: any[] = [];
  usuarios: any[] = [];

  // Modal Detail
  activeModal: string | null = null;
  selectedMovimiento: Movimiento | null = null;

  loading: boolean = false;
  hasSearched: boolean = false;

  constructor(
    private stockService: StockService,
    private rubroService: RubroService,
    private familiaService: FamiliaService,
    private userService: UserService,
    private sedeContextService: SedeContextService
  ) {}

  ngOnInit(): void {
    this.verifySedeContext();
    this.loadRubros();
    this.loadUsuarios();
    
    this.contextSub = this.sedeContextService.sedeEnContexto$.subscribe(() => {
      this.page = 1;
      this.search();
    });
  }

  ngOnDestroy(): void {
    if (this.contextSub) this.contextSub.unsubscribe();
  }

  // Provisional check for Sede
  verifySedeContext() {
    // Current setup uses headers per US
  }

  loadRubros() {
    this.rubroService.getAll().subscribe({
      next: (data: any) => this.rubros = data,
      error: (err: any) => console.error('Error al cargar rubros', err)
    });
  }

  loadFamilias(idRubro: number) {
    this.familiaService.getByRubro(idRubro).subscribe({
      next: (data: any) => this.familias = data,
      error: (err: any) => console.error('Error al cargar familias', err)
    });
  }

  loadUsuarios() {
    this.userService.getAll().subscribe({
      next: (data: any) => this.usuarios = data,
      error: (err: any) => console.error('Error al cargar usuarios', err)
    });
  }

  onRubroChange() {
    this.selectedFamiliaId = null;
    this.familias = [];
    if (this.selectedRubroId) {
      this.loadFamilias(this.selectedRubroId);
    }
    this.searchFormChange();
  }

  searchFormChange() {
    this.page = 1;
    this.search();
  }

  search() {
    this.loading = true;
    this.hasSearched = true;

    this.stockService.getHistorialGlobal(
      this.searchTerm || undefined,
      this.selectedRubroId || undefined,
      this.selectedFamiliaId || undefined,
      this.selectedTipoMovimiento || undefined,
      this.selectedUsuarioId || undefined,
      this.fechaDesde || undefined,
      this.fechaHasta || undefined,
      this.page,
      this.pageSize
    ).subscribe({
      next: (res) => {
        this.movimientos = res.data;
        this.totalCount = res.totalCount;
        this.totalPages = res.totalPages;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error al cargar el historial', err);
        this.loading = false;
      }
    });
  }

  clearFilters() {
    this.searchTerm = '';
    this.selectedRubroId = null;
    this.selectedFamiliaId = null;
    this.selectedTipoMovimiento = '';
    this.selectedUsuarioId = null;
    this.fechaDesde = '';
    this.fechaHasta = '';
    this.familias = [];
    
    this.page = 1;
    this.search();
    this.hasSearched = false;
  }

  prevPage() {
    if (this.page > 1) {
      this.page--;
      this.search();
    }
  }

  nextPage() {
    if (this.page < this.totalPages) {
      this.page++;
      this.search();
    }
  }

  openDetalleModal(movimiento: Movimiento) {
    this.selectedMovimiento = movimiento;
    this.activeModal = 'detalle';
  }

  closeModal() {
    this.activeModal = null;
    this.selectedMovimiento = null;
  }

}
