# Fightyub

Juego de pelea arcade 2D en estilo pixel-art de 16 bits ambientado en Buenos Aires, desarrollado en **Unity 6 (`6000.3.24f1`)**.

## Características

- **11 personajes jugables**, cada uno con su set completo de animaciones y ataque **SUPER** único:
  `Jumaay`, `Tatoon`, `Lila`, `Lalo`, `Luli`, `Pachi`, `Perla`, `Irma`, `Momo`, `Pipi` y `Jere7em`.
- **5 escenarios porteños** con música original en loop continuo:
  - **Obelisco** (Tango arcade)
  - **Triunvirato y Olazábal** (Metal progresivo)
  - **Casa Rosada** (Cumbia arcade)
  - **Floralis Genérica** (Rock nacional argentino)
  - **Puente de la Mujer** (Electrónica urbana)
- **Modos de juego:** Pelea, Versus (2 jugadores) y Torre.
- **Mecánicas de combate:** Defensa perfecta (ventana de 0,15 s con cadena 0/3 → 3/3 y stun al rival), barra de SUPER, hit-stop y efectos de partículas pixel-art optimizados con pooling.
- **Opciones:** Controles independientes de Música y Efectos de sonido (0%–100%), configuración de controles y calidad visual.

## Cómo abrir el proyecto

1. Abrir **Unity Hub** e importar la carpeta raíz del repositorio con **Unity `6000.3.24f1`** (o compatible).
2. Abrir la escena [`Assets/Scenes/SampleScene.unity`](Assets/Scenes/SampleScene.unity).
3. Presionar **Play** en el Editor.

## Estructura principal

- `Assets/Scripts/`: Lógica de combate, gestión de partidas, audio, opciones y sistema de partículas pixel-art.
- `Assets/Chars/`: Spritesheets y metadatos de los 11 luchadores.
- `Assets/Audio/`: Música original en loop y efectos de sonido.
- `Assets/Scenes/`: Escena principal (`SampleScene.unity`).
- `Packages/` y `ProjectSettings/`: Dependencias y configuración de Unity.
