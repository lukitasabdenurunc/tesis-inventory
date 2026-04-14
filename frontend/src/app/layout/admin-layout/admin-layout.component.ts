import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../services/auth';
import { SedeService } from '../../services/sede.service';
import { SedeContextService } from '../../services/sede-context.service';
import { Sede } from '../../models/sede.model';
import { Subscription } from 'rxjs';

import { FormsModule } from '@angular/forms';

@Component({
    selector: 'app-admin-layout',
    standalone: true,
    imports: [CommonModule, RouterModule, FormsModule],
    templateUrl: './admin-layout.component.html',
    styleUrls: ['./admin-layout.component.css']
})
export class AdminLayoutComponent implements OnInit, OnDestroy {
    isConfigMenuOpen = false;
    isInventoryMenuOpen = false;

    isAdmin = false;
    sedes: Sede[] = [];
    selectedSedeId!: number;
    private userSub!: Subscription;
    private contextSub!: Subscription;

    constructor(
        private authService: AuthService,
        private router: Router,
        private sedeService: SedeService,
        private sedeContextService: SedeContextService
    ) { }

    ngOnInit() {
        this.userSub = this.authService.user$.subscribe(user => {
            if (user) {
                // Determine if user is Admin
                const roleValue = user.nombreRol;
                this.isAdmin = roleValue === 'Admin' || roleValue === 'Administrador';

                if (this.isAdmin) {
                    this.loadSedes();
                }
            }
        });

        this.contextSub = this.sedeContextService.sedeEnContexto$.subscribe(sedeId => {
            this.selectedSedeId = sedeId;
        });
    }

    ngOnDestroy() {
        if (this.userSub) this.userSub.unsubscribe();
        if (this.contextSub) this.contextSub.unsubscribe();
    }

    loadSedes() {
        this.sedeService.getAll().subscribe(data => {
            this.sedes = data;
        });
    }

    onSedeChange(event: any) {
        const newSedeId = Number(event.target.value);
        this.sedeContextService.setSedeEnContexto(newSedeId);
    }

    toggleConfigMenu() {
        this.isConfigMenuOpen = !this.isConfigMenuOpen;
    }

    toggleInventoryMenu() {
        this.isInventoryMenuOpen = !this.isInventoryMenuOpen;
    }

    logout() {
        this.authService.logout();
    }
}
