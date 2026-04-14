import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AtributoService } from '../../../services/atributo.service';
import { RubroService } from '../../../services/rubro.service';
import { FamiliaService } from '../../../services/familia.service';
import { Atributo, CreateAtributo, UpdateAtributo, AtributoOpcion, CreateAtributoOpcion, FamiliaAtributo, CreateFamiliaAtributo } from '../../../models/atributo.model';
import { Rubro } from '../../../models/rubro.model';
import { Familia } from '../../../models/familia.model';
import Swal from 'sweetalert2';

@Component({
    selector: 'app-atributos',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './atributos.component.html',
    styleUrls: ['./atributos.component.css']
})
export class AtributosComponent implements OnInit {
    atributos: Atributo[] = [];
    loadingAtributos = false;
    selectedAtributo: Atributo | null = null;

    // Atributo Modal
    showAtributoModal = false;
    isEditAtributo = false;
    currentAtributoForm: CreateAtributo | UpdateAtributo = { codigoAtributo: '', nombre: '', tipoDato: 'STRING', activo: true };
    currentAtributoId: number | null = null;
    tiposDatos = ['STRING', 'NUMBER', 'DECIMAL', 'BOOLEAN', 'LIST'];

    // Opciones LIST
    opciones: AtributoOpcion[] = [];
    nuevaOpcionForm: CreateAtributoOpcion = { codigoOpcion: '', valor: '', activo: true };

    // Asignacion Familia
    rubros: Rubro[] = [];
    familiasSelect: Familia[] = [];
    selectedRubroId: number | null = null;
    selectedFamiliaId: number | null = null;
    atributosDeFamilia: FamiliaAtributo[] = [];
    configObligatorio = false;

    get selectedFamiliaNombre(): string {
        const f = this.familiasSelect.find(fam => fam.idFamilia === this.selectedFamiliaId);
        return f ? f.nombre : '';
    }

    // Modal Asociaciones
    showAsociacionesModal = false;
    familiasAsociadas: FamiliaAtributo[] = [];
    selectedAtributoParaAsociaciones: Atributo | null = null;
    loadingAsociaciones = false;

    constructor(
        private atributoService: AtributoService,
        private rubroService: RubroService,
        private familiaService: FamiliaService
    ) { }

    ngOnInit(): void {
        this.loadAtributos();
        this.loadRubros();
    }

    // ==== Atributo Maestros ====
    loadAtributos(): void {
        this.loadingAtributos = true;
        this.atributoService.getAll(true).subscribe({
            next: (data) => {
                this.atributos = data;
                this.loadingAtributos = false;
            },
            error: () => {
                this.loadingAtributos = false;
                Swal.fire('Error', 'No se pudieron cargar los atributos', 'error');
            }
        });
    }

    selectAtributo(attr: Atributo): void {
        this.selectedAtributo = attr;
        if (attr.tipoDato === 'LIST') {
            this.loadOpciones(attr.idAtributo);
        }
        // Si queremos listar las asignaciones por atributo, sería otro endpoint. 
        // Por ahora, la Asignación la manejamos seleccionando una familia y viendo qué atributos tiene asignados.
    }

    openCreateModal(): void {
        this.isEditAtributo = false;
        this.currentAtributoForm = { codigoAtributo: '', nombre: '', tipoDato: 'STRING', unidad: '', descripcion: '', activo: true };
        this.showAtributoModal = true;
    }

    openEditModal(attr: Atributo, event: Event): void {
        event.stopPropagation();
        this.isEditAtributo = true;
        this.currentAtributoId = attr.idAtributo;
        this.currentAtributoForm = { ...attr };
        this.showAtributoModal = true;
    }

    closeAtributoModal(): void {
        this.showAtributoModal = false;
    }

    saveAtributo(): void {
        if (!this.currentAtributoForm.codigoAtributo || !this.currentAtributoForm.nombre) {
            Swal.fire('Atención', 'Código y Nombre obligatorios', 'warning');
            return;
        }

        if (this.isEditAtributo && this.currentAtributoId) {
            this.atributoService.update(this.currentAtributoId, this.currentAtributoForm as UpdateAtributo).subscribe({
                next: (res) => {
                    this.loadAtributos();
                    if (this.selectedAtributo?.idAtributo === this.currentAtributoId) this.selectedAtributo = res;
                    this.closeAtributoModal();
                    Swal.fire('Éxito', 'Atributo actualizado', 'success');
                },
                error: (err) => Swal.fire('Error', err.error?.message, 'error')
            });
        } else {
            this.atributoService.create(this.currentAtributoForm as CreateAtributo).subscribe({
                next: () => {
                    this.loadAtributos();
                    this.closeAtributoModal();
                    Swal.fire('Éxito', 'Atributo creado', 'success');
                },
                error: (err) => Swal.fire('Error', err.error?.message, 'error')
            });
        }
    }

    deleteAtributo(id: number, event: Event): void {
        event.stopPropagation();
        Swal.fire({
            title: '¿Desactivar Atributo?',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Sí'
        }).then((result) => {
            if (result.isConfirmed) {
                this.atributoService.delete(id).subscribe({
                    next: () => {
                        if (this.selectedAtributo?.idAtributo === id) this.selectedAtributo = null;
                        this.loadAtributos();
                        Swal.fire('Baja Exitosa', '', 'success');
                    },
                    error: (err) => Swal.fire('Error', err.error?.message, 'error')
                });
            }
        });
    }

    // ==== Opciones LIST ====
    loadOpciones(idAtributo: number): void {
        this.atributoService.getOpciones(idAtributo).subscribe({
            next: (data) => this.opciones = data,
            error: () => Swal.fire('Error', 'No se pudieron cargar opciones', 'error')
        });
    }

    addOpcion(): void {
        if (!this.selectedAtributo || this.selectedAtributo.tipoDato !== 'LIST') return;
        if (!this.nuevaOpcionForm.codigoOpcion || !this.nuevaOpcionForm.valor) {
            Swal.fire('Atención', 'Datos de opción incompletos', 'warning');
            return;
        }

        this.atributoService.addOpcion(this.selectedAtributo.idAtributo, this.nuevaOpcionForm).subscribe({
            next: () => {
                this.loadOpciones(this.selectedAtributo!.idAtributo);
                this.nuevaOpcionForm = { codigoOpcion: '', valor: '', activo: true };
            },
            error: (err) => Swal.fire('Error', err.error?.message, 'error')
        });
    }

    deleteOpcion(idOpcion: number): void {
        this.atributoService.deleteOpcion(idOpcion).subscribe({
            next: () => {
                if (this.selectedAtributo) this.loadOpciones(this.selectedAtributo.idAtributo);
            },
            error: (err) => Swal.fire('Error', err.error?.message, 'error')
        });
    }

    // ==== Configuración Familias ====
    loadRubros(): void {
        this.rubroService.getAll(false).subscribe({
            next: (data) => this.rubros = data
        });
    }

    onRubroChange(): void {
        this.selectedFamiliaId = null;
        this.atributosDeFamilia = [];
        if (this.selectedRubroId) {
            this.familiaService.getByRubro(this.selectedRubroId, false).subscribe({
                next: (data) => this.familiasSelect = data
            });
        } else {
            this.familiasSelect = [];
        }
    }

    onFamiliaChange(): void {
        if (this.selectedFamiliaId) {
            this.loadAtributosDeFamilia(this.selectedFamiliaId);
        } else {
            this.atributosDeFamilia = [];
        }
    }

    loadAtributosDeFamilia(idFamilia: number): void {
        this.atributoService.getAtributosDeFamilia(idFamilia).subscribe({
            next: (data) => this.atributosDeFamilia = data
        });
    }

    asignarAtributoAFamilia(): void {
        if (!this.selectedFamiliaId || !this.selectedAtributo) {
            Swal.fire('Info', 'Seleccioná una familia arriba y un atributo de la lista principal para asignar.', 'info');
            return;
        }

        const dto = {
            idAtributo: this.selectedAtributo.idAtributo,
            obligatorio: this.configObligatorio,
            activo: true
        };

        this.atributoService.assignToFamilia(this.selectedFamiliaId, dto).subscribe({
            next: () => {
                this.loadAtributosDeFamilia(this.selectedFamiliaId!);
                Swal.fire('Éxito', 'Atributo configurado a familia', 'success');
            },
            error: (err) => Swal.fire('Error', err.error?.message, 'error')
        });
    }

    removerAtributoDeFamilia(idAtributo: number): void {
        if (!this.selectedFamiliaId) return;
        this.atributoService.removeFromFamilia(this.selectedFamiliaId, idAtributo).subscribe({
            next: () => this.loadAtributosDeFamilia(this.selectedFamiliaId!),
            error: (err) => Swal.fire('Error', err.error?.message, 'error')
        });
    }

    // ==== Ver Asociaciones ====
    openAsociacionesModal(attr: Atributo, event: Event): void {
        event.stopPropagation();
        this.selectedAtributoParaAsociaciones = attr;
        this.showAsociacionesModal = true;
        this.loadFamiliasAsociadas(attr.idAtributo);
    }

    closeAsociacionesModal(): void {
        this.showAsociacionesModal = false;
        this.selectedAtributoParaAsociaciones = null;
        this.familiasAsociadas = [];
    }

    loadFamiliasAsociadas(idAtributo: number): void {
        this.loadingAsociaciones = true;
        this.atributoService.getFamiliasDeAtributo(idAtributo).subscribe({
            next: (data) => {
                this.familiasAsociadas = data;
                this.loadingAsociaciones = false;
            },
            error: () => {
                this.loadingAsociaciones = false;
                Swal.fire('Error', 'No se pudieron cargar las asociaciones', 'error');
            }
        });
    }

    desasociarFamiliaDesdeModal(idFamilia: number, idAtributo: number, esObligatorio: boolean): void {
        const textWarning = esObligatorio
            ? 'Se perderán todos los datos guardados en ese atributo para los productos de esta familia. IMPORTANTE: Como el atributo es obligatorio, se regenerarán los SKUs de todos los productos de esta familia. ¿Continuar?'
            : 'Se perderán todos los datos guardados en ese atributo para los productos de esta familia. ¿Continuar?';

        Swal.fire({
            title: '¿Desasociar de familia?',
            text: textWarning,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Sí, desasociar',
            cancelButtonText: 'Cancelar'
        }).then((result) => {
            if (result.isConfirmed) {
                this.atributoService.removeFromFamilia(idFamilia, idAtributo).subscribe({
                    next: () => {
                        this.loadFamiliasAsociadas(idAtributo);
                        // Si da la casualidad q la familia seleccionada abajo es esta misma, la actualizamos
                        if (this.selectedFamiliaId === idFamilia) {
                            this.loadAtributosDeFamilia(this.selectedFamiliaId);
                        }
                        Swal.fire('Éxito', 'Atributo desasociado correctamente', 'success');
                    },
                    error: (err) => Swal.fire('Error', err.error?.message, 'error')
                });
            }
        });
    }
}
