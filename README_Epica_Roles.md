6.X Épica: Gestión de Roles

────────────────────────────────────────
6.1 Descripción general de la épica
────────────────────────────────────────
Esta épica extiende el módulo de Configuración para permitir la administración dinámica de los Roles del sistema.
- **Objetivo**: Permitir crear, editar y eliminar roles, asegurando la integridad referencial (no eliminar roles asignados).
- **Relación con el problema**: Provee flexibilidad para ajustar los perfiles de acceso sin intervención en base de datos.

────────────────────────────────────────
6.2 Alcance funcional de la épica
────────────────────────────────────────
**Funcionalidades incluidas:**
1.  Listado de Roles existentes (reutilizando diseño de tabla de Usuarios).
2.  Alta de nuevos Roles.
3.  Edición de Roles (Nombre, Descripción).
4.  Eliminación de Roles con validación de dependencia (no se elimina si tiene usuarios).

**Suposiciones o restricciones:**
- Los roles "Administrador" pueden ser editados pero debería tenerse cuidado.
- La validación de eliminación es estricta.

────────────────────────────────────────
6.3 Historias de usuario asociadas
────────────────────────────────────────
- **HU-ROLES-01**: Como Administrador quiero ver la lista de roles para conocer los perfiles disponibles.
- **HU-ROLES-02**: Como Administrador quiero crear/editar roles para ajustar las descripciones o nombres.
- **HU-ROLES-03**: Como Administrador quiero eliminar roles en desuso, pero el sistema debe impedirlo si hay usuarios asignados.

────────────────────────────────────────
6.4 Diseño técnico asociado
────────────────────────────────────────
**Componentes de Frontend (Angular):**
- `RolesListComponent`: Reutiliza diseño de `UsersListComponent`.
- `RoleFormComponent`: Formulario para ABM de Roles.
- `RoleService`: Métodos CRUD (`create`, `update`, `delete`, `getAll`, `getById`).

**Lógica de Backend (C# .NET):**
- `RolesController`: Endpoints `POST`, `PUT`, `DELETE`.
- `RolesService`: Lógica de validación (`DeleteRoleAsync` verifica `Usuarios.Any()`).
- `RoleRepository`: Implementación de persistencia con `Include(Usuarios)`.

────────────────────────────────────────
6.5 Pruebas realizadas (Verificación)
────────────────────────────────────────
| Acción | Resultado Esperado |
| :--- | :--- |
| Acceder a `/configuration/roles` | Ver lista de roles. |
| Crear Rol "Test" | Aparece en lista. |
| Eliminar Rol "Test" | Se elimina correctamente. |
| Eliminar Rol "Admin" (Asignado) | **Error**: "No se puede eliminar un rol asignado a un usuario." |

────────────────────────────────────────
6.6 Decisiones de Implementación y Ajustes
────────────────────────────────────────
**[2026-04-14]**
- **Descripción del cambio**: Configuración del rol "Administrador" como visualizador de múltiples sedes, gestionando el cambio de sede mediante un menú contextual reactivo (SedeContextService).
- **Motivo técnico**: El rol administrador requería de la capacidad de alternar entre los contextos de stock y transferencias de cada sede directamente desde el front, sin modificar la estructura base de las tablas de datos vinculadas por idSede y enviando la nueva cabecera 'Sede-Contexto' configurada de manera global a través de un interceptor.
- **Impacto funcional**: Impacta de manera positiva la gestión de los administradores proveyendo una UI transparente global en el TopBar. El resto de los roles sigue restringido al idSede embebido en su propio claim original sin romper las lógicas pre-establecidas.
