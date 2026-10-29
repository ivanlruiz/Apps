# BichiKart!

Juego 3D de karts donde bichitos diminutos se pelean por una comida gigante dentro de una casa que flota en el espacio.

- [`DISENO.md`](DISENO.md): documento de diseño con todas las decisiones.
- [`BichiKart/`](BichiKart/): proyecto de Unity 6 (URP).

## Estado

**Prototipo en construcción** (etapa 2). En la escena `Prototipo` ya se maneja el kart de Blip por la Cocina, una sala cerrada hecha con formas simples. Cómo probarlo y qué falta: [`HISTORIAL.md`](HISTORIAL.md), secciones 6 y 9.

La escena `SalaDePrueba` es la del proyecto base: un rincón de la cocina para ver la luz y los materiales, sin manejo.

## Bajarlo a tu PC (por ejemplo, al disco A:)

Necesitás [Git para Windows](https://git-scm.com/download/win). Abrí la terminal (`cmd`) y escribí:

```bat
A:
cd \
git clone --branch ccr-5e101442-8k3tts https://github.com/ivanlruiz/Apps.git
```

Si te pide iniciar sesión en GitHub, se abre el navegador para hacerlo.

El proyecto queda en `A:\Apps\bichikart\BichiKart`.

Para traer cambios nuevos más adelante:

```bat
cd /d A:\Apps
git pull
```

**Sin Git:** en la página del repo en GitHub, elegí la rama `ccr-5e101442-8k3tts`, tocá **Code → Download ZIP** y descomprimilo en `A:\`.

## Abrirlo en Unity

1. Abrí **Unity Hub** → **Projects** → **Add** → **Add project from disk**.
2. Elegí la carpeta `A:\Apps\bichikart\BichiKart`.
3. El proyecto usa **Unity 6000.3.14f1** (Unity 6.3 LTS). Si tenés otra versión de Unity 6, elegila en la columna "Editor Version" y aceptá actualizar el proyecto.
4. La primera vez tarda varios minutos, porque Unity descarga los paquetes y arma la carpeta `Library`.
5. En la ventana **Project**, abrí `Assets/Scenes/Menu` con doble clic.
6. Apretá **Play**, elegí **Partida rápida** y el nivel de la compu. Hacé clic en el juego y manejá con W/S/A/D; el mouse mueve la cámara y Escape pone pausa.

## Estructura

| Carpeta | Qué hay |
|---|---|
| `BichiKart/Assets/Scenes` | Las escenas `Menu`, `Prototipo` y `SalaDePrueba` |
| `BichiKart/Assets/BichiKart/Scripts` | El código del juego: el kart, la sala, la cámara, la partida, la compu, la interfaz, el menú y las formas simples |
| `BichiKart/Assets/BichiKart/Editor` | Herramientas del editor: armar las escenas Menu y Prototipo, y correr las pruebas |
| `BichiKart/Assets/BichiKart/Pruebas` | Las pruebas automáticas |
| `BichiKart/Assets/BichiKart/Ajustes` | Los números del manejo, de la partida, de cada nivel de la compu y las medidas de la sala |
| `BichiKart/Assets/BichiKart/Resources/Bichitos` | Los modelos 3D de los seis bichitos (`.fbx`) |
| `blender` | El script de Blender que arma los bichitos y los exporta a Unity (ver HISTORIAL, sección 9) |
| `BichiKart/Assets/Settings` | La configuración de URP (PC y celular) y los efectos de imagen |
| `BichiKart/Packages` | Los paquetes de Unity que usa el proyecto |
| `BichiKart/ProjectSettings` | La configuración del proyecto (nombre, identificador, etc.) |
