import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { SedeContextService } from '../services/sede-context.service';

export const sedeInterceptor: HttpInterceptorFn = (req, next) => {
    // Only intercept requests going to the backend API that might need context (all of them except external auth)
    if (req.url.includes('/api/')) {
        const sedeContextService = inject(SedeContextService);
        const sedeId = sedeContextService.getSedeEnContextoVal();
        
        const cloned = req.clone({
            setHeaders: {
                'Sede-Contexto': sedeId.toString()
            }
        });
        return next(cloned);
    }
    
    return next(req);
};
