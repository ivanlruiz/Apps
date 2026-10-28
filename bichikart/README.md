# BichiKart!

Juego 3D de karts donde bichitos diminutos se pelean por una comida gigante dentro de una casa que flota en el espacio.

- [`DISENO.md`](DISENO.md): documento de diseño con todas las decisiones.
- [`BichiKart/`](BichiKart/): proyecto de Unity 6 (URP).

## Estado

Proyecto **base**: abre en Unity 6 con URP configurado y tiene una **sala de prueba**. La sala es un rincón de la cocina con la mesada, la pileta, la heladera gigante, la ventana al espacio, las rejillas de los caños, el kart de Blip y la rebanada de pan, todo con formas simples. Todavía no se maneja: eso llega con el prototipo.

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
3. El proyecto está hecho con **Unity 6000.0.62f1 LTS**. Si tenés otra versión de Unity 6, elegila en la columna "Editor Version" y aceptá actualizar el proyecto.
4. La primera vez tarda varios minutos, porque Unity descarga los paquetes y arma la carpeta `Library`.
5. En la ventana **Project**, abrí `Assets/Scenes/SalaDePrueba` con doble clic.
6. Apretá **Play**: la rebanada de pan flota y gira, y la antena de Blip se mueve.

## Estructura

| Carpeta | Qué hay |
|---|---|
| `BichiKart/Assets/Scenes` | La escena `SalaDePrueba` |
| `BichiKart/Assets/BichiKart/Scripts/Prueba` | El código que arma la sala de prueba con formas simples |
| `BichiKart/Assets/Settings` | La configuración de URP (PC y celular) y los efectos de imagen |
| `BichiKart/Packages` | Los paquetes de Unity que usa el proyecto |
| `BichiKart/ProjectSettings` | La configuración del proyecto (nombre, identificador, etc.) |
