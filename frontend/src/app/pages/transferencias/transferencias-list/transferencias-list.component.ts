import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TransferenciaService } from '../../../services/transferencia.service';
import { SedeService } from '../../../services/sede.service';
import { StockService } from '../../../services/stock.service';
import { ProductoService } from '../../../services/producto.service';
import { AuthService } from '../../../services/auth';
import { Transferencia, MotivoTransferencia } from '../../../models/transferencia.model';
import { TransferenciaCardComponent } from '../transferencia-card/transferencia-card.component';
import { SedeContextService } from '../../../services/sede-context.service';
import { Subscription } from 'rxjs';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-transferencias-list',
  standalone: true,
  imports: [CommonModule, FormsModule, TransferenciaCardComponent],
  templateUrl: './transferencias-list.component.html',
  styleUrls: []
})
export class TransferenciasListComponent implements OnInit {
  activeTab: 'entrantes' | 'salientes' = 'entrantes';
  entrantes: Transferencia[] = [];
  salientes: Transferencia[] = [];
  isLoading = false;
  isCreatorSede = false; 
  private contextSub!: Subscription;

  // Modal Properties
  isModalOpen = false;
  sedes: any[] = [];
  stockList: any[] = [];
  filteredStock: any[] = [];
  searchProduct = '';
  showDropdown = false;
  MotivoEnum = MotivoTransferencia;

  newRequest = {
    idSedeOrigen: null as number | null,
    idProducto: null as number | null,
    cantidad: 1,
    motivo: MotivoTransferencia.Definitiva,
    observaciones: ''
  };
  selectedProductoStock: any = null;

  // View Details
  isViewModalOpen = false;
  selectedViewTransferencia: Transferencia | null = null;
  selectedViewStockResult: any = null;
  isLoadingViewStock = false;

  constructor(
    private transferenciaService: TransferenciaService,
    private sedeService: SedeService,
    private stockService: StockService,
    private productoService: ProductoService,
    private authService: AuthService,
    private sedeContextService: SedeContextService
  ) {}

  ngOnInit(): void {
    // Suscribirse a cambios de sede para recargar
    this.contextSub = this.sedeContextService.sedeEnContexto$.subscribe(() => {
      this.cargarDatos();
    });
  }

  ngOnDestroy(): void {
    if (this.contextSub) this.contextSub.unsubscribe();
  }

  cambiarPestana(tab: 'entrantes' | 'salientes') {
    this.activeTab = tab;
  }

  cargarDatos() {
    this.isLoading = true;
    
    this.transferenciaService.getEntrantes().subscribe({
      next: (res) => { this.entrantes = res; },
      error: (err) => { console.error(err); }
    });

    this.transferenciaService.getSalientes().subscribe({
      next: (res) => { this.salientes = res; this.isLoading = false; },
      error: (err) => { console.error(err); this.isLoading = false; }
    });
  }

  // --- Modal Logic ---
  openModal() {
    this.isModalOpen = true;
    this.resetForm();
    this.loadSedes();
    // No cargar stock hasta elegir Sede Origen
    this.stockList = [];
    this.filteredStock = [];
  }

  closeModal() {
    this.isModalOpen = false;
  }

  resetForm() {
    this.newRequest = {
      idSedeOrigen: null,
      idProducto: null,
      cantidad: 1,
      motivo: MotivoTransferencia.Definitiva,
      observaciones: ''
    };
    this.searchProduct = '';
    this.selectedProductoStock = null;
  }

  loadSedes() {
    let userSedeId = 0;
    this.authService.user$.subscribe(u => { if(u?.idSede) userSedeId = u.idSede; }).unsubscribe();

    this.sedeService.getAll().subscribe(res => {
      // Exclude my Sede
      this.sedes = res.filter(s => s.idSede !== userSedeId);
    });
  }

  onSedeChange() {
    this.searchProduct = '';
    this.selectedProductoStock = null;
    
    if (!this.newRequest.idSedeOrigen) {
        this.stockList = [];
        this.filteredStock = [];
        return;
    }

    // Cargar todos los productos activos y el stock en paralelo
    Promise.all([
      this.productoService.getAll(false).toPromise(),
      this.stockService.getStockSede('', undefined, undefined, true, undefined, 1, 1000, this.newRequest.idSedeOrigen).toPromise()
    ]).then(([productosRes, stockRes]: [any, any]) => {
      const allProductos = productosRes || [];
      const stockArray = stockRes?.data || stockRes || [];

      // Mapear productos e inyectar stock de la sede si lo tiene, sino Cantidad = 0
      this.stockList = allProductos.map((p: any) => {
        const matchingStock = stockArray.find((s: any) => s.idProducto === p.idProducto);
        return {
          idProducto: p.idProducto,
          sku: p.sku,
          nombreProducto: p.nombre,
          unidadMedida: p.unidadMedida || 'Unidades',
          cantidadActual: matchingStock ? matchingStock.cantidadActual : 0,
          conBajoStock: matchingStock ? (matchingStock.cantidadActual <= matchingStock.puntoReposicion) : true
        };
      });

      this.filteredStock = [...this.stockList];
    });
  }

  onSearchProduct() {
    const term = this.searchProduct.toLowerCase();
    this.filteredStock = this.stockList.filter(s => 
      s.nombreProducto?.toLowerCase().includes(term) || 
      s.sku?.toLowerCase().includes(term)
    );
  }

  selectProduct(stockItem: any) {
    this.selectedProductoStock = stockItem;
    this.newRequest.idProducto = stockItem.idProducto;
    this.searchProduct = `${stockItem.sku} - ${stockItem.nombreProducto}`;
    this.showDropdown = false;
  }

  submitRequest() {
    if (!this.newRequest.idSedeOrigen || !this.newRequest.idProducto || this.newRequest.cantidad <= 0) {
      Swal.fire('Error', 'Debe completar los campos obligatorios.', 'error');
      return;
    }

    const sedeOrigenStr = this.sedes.find(s => s.idSede == this.newRequest.idSedeOrigen)?.nombreSede || 'Desconocida';
    const motivoStr = this.newRequest.motivo == MotivoTransferencia.Prestamo ? 'Préstamo' : 'Definitiva';
    const stockResultante = this.selectedProductoStock.cantidadActual - this.newRequest.cantidad;
    const warning = stockResultante < 0 ? '<br><p class="text-yellow-600 font-bold mt-2">¡Advertencia! La Sede seleccionada no tiene stock físico suficiente para cubrir este pedido ahora mismo.</p>' : '';

    Swal.fire({
      title: 'Confirmar Solicitud',
      html: `
        <div class="text-left space-y-2 text-sm mt-3">
          <p><strong>Solicitar a Sede:</strong> ${sedeOrigenStr}</p>
          <p><strong>Producto:</strong> ${this.selectedProductoStock.sku} - ${this.selectedProductoStock.nombreProducto} (${this.selectedProductoStock.unidadMedida || 'Unidades'})</p>
          <p><strong>Motivo:</strong> ${motivoStr}</p>
          <div class="border-t my-2 pt-2"></div>
          <p><strong>Stock en Destino:</strong> ${this.selectedProductoStock.cantidadActual}</p>
          <p><strong>Cantidad Pedida:</strong>  <span class="text-red-500 font-bold">+ ${this.newRequest.cantidad}</span></p>
          ${warning}
        </div>
      `,
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Enviar Solicitud',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#4f46e5',
      cancelButtonColor: '#9ca3af'
    }).then((result) => {
      if (result.isConfirmed) {
        // Enviar
        let userSedeId = 1; 
        this.authService.user$.subscribe(u => { if(u?.idSede) userSedeId = u.idSede; }).unsubscribe();

        this.transferenciaService.create({
          idProducto: this.newRequest.idProducto!,
          idSedeDestino: userSedeId, // Yo soy el destino, lo pido para mí
          idSedeOrigen: this.newRequest.idSedeOrigen!, // Ellos me lo dan
          cantidad: this.newRequest.cantidad,
          motivo: Number(this.newRequest.motivo),
          observaciones: this.newRequest.observaciones
        }).subscribe({
          next: () => {
            Swal.fire('Completado', 'Solicitud creada con éxito', 'success');
            this.closeModal();
            this.cargarDatos(); // Refresh lists
            this.activeTab = 'salientes'; // Because we created a outgoing request
          },
          error: (err) => {
            console.error(err);
            const backendMsg = err.error?.message || err.message;
            const backendDetails = err.error?.details || '';
            Swal.fire('Error', `<p>No se pudo crear la solicitud.</p><br><p class="text-sm text-red-600">${backendMsg}</p><br><textarea class="w-full text-xs text-gray-500 font-mono h-24" readonly>${backendDetails}</textarea>`, 'error');
          }
        });
      }
    });
  }

  // --- Actions Logic ---
  aceptar(t: Transferencia) {
    if(confirm(`¿Desea aceptar la transferencia de ${t.cantidad} ${t.nombreProducto}?`)) {
      this.transferenciaService.aceptar(t.idTransferencia, 'Aceptado desde listado').subscribe({
        next: () => {
          Swal.fire('Aceptada', 'La solicitud pasó a En Tránsito', 'success');
          this.cargarDatos();
        },
        error: (e) => Swal.fire('Error', 'No se pudo aceptar', 'error')
      });
    }
  }

  rechazar(t: Transferencia) {
    const motivo = prompt('Motivo de rechazo:');
    if(motivo !== null) {
      this.transferenciaService.rechazar(t.idTransferencia, motivo).subscribe({
        next: () => this.cargarDatos(),
        error: (e) => Swal.fire('Error', 'No se pudo rechazar', 'error')
      });
    }
  }

  recibir(t: Transferencia) {
    if(confirm(`¿Confirma la recepción de ${t.cantidad} ${t.nombreProducto}? Esto impactará el stock.`)) {
      this.transferenciaService.confirmarRecepcion(t.idTransferencia, 'Recibido').subscribe({
        next: () => {
          Swal.fire('Recibido', 'Stock actualizado en destino', 'success');
          this.cargarDatos();
        },
        error: (e) => Swal.fire('Error', 'No se pudo confirmar', 'error')
      });
    }
  }

  devolver(t: Transferencia) {
    if(confirm(`¿Desea devolver el préstamo de ${t.cantidad} ${t.nombreProducto}?`)) {
      this.transferenciaService.devolver(t.idTransferencia, 'Devolución').subscribe({
        next: () => {
          Swal.fire('Devuelto', 'Se ha devuelto el stock a la sede origen', 'success');
          this.cargarDatos();
        },
        error: (e) => Swal.fire('Error', 'Error al devolver', 'error')
      });
    }
  }

  verDetalles(t: Transferencia) {
    this.selectedViewTransferencia = t;
    this.isViewModalOpen = true;
    this.selectedViewStockResult = null;
    this.isLoadingViewStock = true;

    // Buscar en el stock en vivo de la Sede Origen
    this.stockService.getStockSede('', undefined, undefined, true, undefined, 1, 1000, t.idSedeOrigen).subscribe({
      next: (res) => {
        const list = res.data || res;
        this.selectedViewStockResult = list.find((s: any) => s.idProducto === t.idProducto) || { cantidadActual: 0 };
        this.isLoadingViewStock = false;
      },
      error: () => {
        this.selectedViewStockResult = { cantidadActual: '?' };
        this.isLoadingViewStock = false;
      }
    });
  }

  closeViewModal() {
    this.isViewModalOpen = false;
    this.selectedViewTransferencia = null;
  }
}
