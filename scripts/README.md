# scripts/

Utilidades del entorno local. **Todas asumen que se ejecutan desde cualquier
directorio**: resuelven la raíz del repo subiendo un nivel desde acá.

El punto de entrada del día a día es **`astralrental-local.cmd`, en la raíz** —
un menú que llama a estos scripts. Casi nunca hace falta ejecutarlos a mano.

| Script | Para qué | Notas |
|---|---|---|
| `run.ps1` | Orquesta Docker: `up`, `rebuild`, `down`, `logs`, `reset-db`, `seed-superuser` | Es el motor detrás del menú |
| `run-emulator.ps1` | Levanta el stack + emulador Android e instala el APK | `-Rebuild` recompila la app antes |
| `allow-lan-access.ps1` | Abre el firewall (8080/8443) para probar en iPhone o Android real | **Requiere administrador**. Solo perfil Privado |
| `expose-public.ps1` | Túnel de Cloudflare con HTTPS para acceder desde cualquier red | `-Stop` lo cierra. La URL cambia en cada arranque |
| `astralrental-ios.sh` | Compila e instala la app en un iPhone | **Solo macOS.** Ver `GlobalGuidelines/COMPILAR_IOS_EN_MAC.md` |

## Al mover o agregar un script

Los `.ps1` y `.sh` que necesiten rutas del repo **no deben asumir que están en
la raíz**. El patrón correcto:

```powershell
# PowerShell
$RepoRoot = Split-Path -Parent $PSScriptRoot
```

```bash
# Bash
REPO_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
```

⚠️ `.gitattributes` fuerza **LF en los `.sh` y CRLF en los `.ps1`/`.cmd`**. Un
`.sh` con CRLF falla en macOS con `bad interpreter: No such file or directory`.
