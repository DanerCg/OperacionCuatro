# OPERACIÓN CUATRO

Juego de acción y plataformas 2D para 1 o 2 jugadores, hecho en **Unity 6.3 LTS (6000.3.11f1)** con URP 2D.

Se elige entre 4 personajes (**Danner, Cesar, Sebastián e Inés**), se avanza por las misiones disparando,
lanzando granadas y recogiendo armas, y se cumple el objetivo de cada nivel (en el Nivel 2, destruir el
generador central). Por ahora están implementados los niveles **Level01** y **Level02**; se irán agregando más.

## Equipo

| Integrante | Área |
|---|---|
| Daner | Base del proyecto, núcleo (Core), configuración y herramientas de editor |
| Cesar | Jugador y combate (personajes, animaciones, armas, efectos) |
| Castillo | Enemigos (scripts, sprites, animaciones y prefabs) |
| Condor | Mundo y niveles (scripts de nivel, fondos, props, escenas Level01 y Level02) |
| Inés | Interfaz (UI) y audio, escenas de menú, selección de personaje y resultados |

## Cómo abrir el proyecto

1. Instalar **Unity Hub** y la versión **6000.3.11f1** (Unity 6.3 LTS).
2. Clonar este repositorio: `git clone <URL-del-repo>`
3. En Unity Hub: *Projects → Add → Add project from disk* y elegir la carpeta clonada.
4. Esperar a que Unity importe todo (la primera vez tarda varios minutos y necesita internet).
5. Abrir `Assets/OperacionCuatro/Scenes/MainMenu` y pulsar Play.

> Nota: `Packages/manifest.json` incluye dos paquetes descargados desde GitHub por URL
> (`com.coplaydev.unity-mcp` y `com.ramenvr.aura-unity`, herramientas de asistencia en el editor).
> El código del juego no depende de ellos. Si Unity muestra errores de paquetes (por ejemplo, si
> no tienen Git instalado), pueden quitar esas dos líneas de `Packages/manifest.json`.

## Estructura

```
Assets/OperacionCuatro/
├── Scripts/    Core · Player · Combat · Enemies · Level · UI
├── Editor/     Builders y herramientas de prueba del editor
├── Scenes/     MainMenu · CharacterSelection · Level01 · Level02 · Results
├── Prefabs/    combate (balas, bombas, efectos) y objetos del nivel 2
├── Sprites/    personajes, enemigos, efectos, props y mundo
├── Animations/ animaciones y controladores de personajes y enemigos
├── Audio/      audios maestros de la música
└── Resources/  personajes, enemigos, retratos y audio cargados en ejecución
```

## Cómo se organizó el trabajo

El juego se desarrolló previamente, con conocimiento del docente. Para el repositorio se
dividió en 5 partes, y cada integrante sube la suya desde su propia cuenta de GitHub,
con su propia rama y un Pull Request revisado por otro compañero.

| Parte | Rama | Responsable |
|---|---|---|
| 1. Base del proyecto | `main` / `develop` | Daner |
| 2. Jugador y combate | `feature/parte-2-jugador` | Cesar |
| 3. Enemigos | `feature/parte-3-enemigos` | Castillo |
| 4. Mundo y niveles 1 y 2 | `feature/parte-4-mundo-niveles` | Condor |
| 5. UI y audio | `feature/parte-5-ui-audio` | Inés |

Los niveles que faltan se agregarán en partes nuevas, con el mismo flujo de ramas y Pull Requests.

Ver `CONTRIBUTING.md` para las reglas de trabajo.

## Aviso sobre herramientas de IA

Parte del arte, el audio y el código del proyecto se generó con apoyo de herramientas de inteligencia
artificial (ver los scripts de `Assets/OperacionCuatro/Editor/Builders`).
