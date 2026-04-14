import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class SedeContextService {
  private sedeEnContextoSubject = new BehaviorSubject<number>(this.getInitialSedeId());
  public sedeEnContexto$ = this.sedeEnContextoSubject.asObservable();

  constructor() { }

  private getInitialSedeId(): number {
    // Intentar obtener sede previamente seleccionada
    const savedSede = localStorage.getItem('inventory_sede_context');
    if (savedSede) {
      const parsed = parseInt(savedSede, 10);
      if (!isNaN(parsed)) return parsed;
    }

    // Fallback al usuario original
    let sedeId = 1; 
    const userStr = localStorage.getItem('inventory_user');
    if (userStr) {
      try {
        const user = JSON.parse(userStr);
        if (user.idSede) {
          sedeId = Number(user.idSede);
        }
      } catch (e) {
        console.error('Error parseando usuario de localStorage', e);
      }
    }
    return sedeId;
  }

  setSedeEnContexto(sedeId: number) {
    if (this.sedeEnContextoSubject.getValue() !== sedeId) {
      this.sedeEnContextoSubject.next(sedeId);
      localStorage.setItem('inventory_sede_context', sedeId.toString());
    }
  }

  getSedeEnContextoVal(): number {
    return this.sedeEnContextoSubject.getValue();
  }
}
