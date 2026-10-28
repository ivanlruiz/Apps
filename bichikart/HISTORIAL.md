# BichiKart! — Historial completo de la conversación

Este archivo resume **todo** lo que se habló en la primera conversación sobre el juego, de principio a fin, para poder seguir en una conversación nueva sin perder nada.

- El **diseño ordenado** está en [`DISENO.md`](DISENO.md). Si algo de este historial choca con `DISENO.md`, vale lo último que se decidió, que está marcado acá.
- Las **imágenes** están en [`imagenes/`](imagenes/).

---

## 1. Cómo quiere trabajar el usuario (importante)

- Hablarle en **español rioplatense** (voseo).
- **Al principio quería que se le preguntara todo.** Desde la etapa 2 cambió: Claude **avanza solo con lo técnico** (programar, probar en Unity, commit y push al cerrar cada hito) y le pregunta **solo cosas de diseño y mecánicas** del juego, porque quiere poder estar ausente mientras Claude trabaja. Cuando una duda de diseño no frena, Claude elige algo razonable y fácil de cambiar, y lo consulta después.
- Prefiere **muchas preguntas** con opciones concretas para decidir.
- **Sabe programar en C#**, pero hay que explicarle paso a paso lo de Unity, Git y la terminal.
- Usa **Gemini** para los conceptos visuales y los trae para revisarlos.
- Le preocupa el **límite de uso**: no gastar en cosas que no pidió.
- Trabaja en una **PC con Windows**. El repo está en **`A:\Apps`**.
- Estilo visual que le gusta: **acabado suave y pulido tipo película animada** (luz cálida, sombras suaves, materiales tipo plástico). Ver `imagenes/referencia-graficos.png`.

## 2. El pedido original

Texto del usuario:

> Juego de pelotita y gravedad. Me imaginé como unos kartings y una miguita de pan, queso, galletita, lo que sea, temática en el espacio, en una casa, kartings (ruedas negras, colores personalizados), personajes (bichitos raros del espacio desbloqueables y/o personalizables).
>
> Empieza una ronda en un nivel o una sala, lo que sea; los dos jugadores están en los extremos y en el medio hay comida.
>
> Cuando el jugador toca la comidita se le pone arriba y tiene que aguantar 15-20 segundos para ganar, pero vas a ir más lento, en lo que tu oponente tiene la ventaja de ser más rápido y de poder chocarte y robarte la comida, y el tiempo se reinicia.
>
> Ahora, la gravedad es otra cosa: la sala, al ser cerrada (piso, pared y techo), podés "pegarte" en la pared o el techo así no te puede atrapar la otra persona.
>
> Diseño del escenario con tuberías en el nivel para darle más diversión a la persecución.

Después aclaró: **"Es un juego que quiero hacer en Unity, puede ser para celular y/o PC"**.

## 3. Cronología

### 3.1 Error inicial: un juego 2D hecho sin preguntar

- Claude arrancó a programar **sin preguntar**: primero pensó una versión web y, cuando el usuario dijo Unity, armó un **proyecto de Unity en 2D** (vista de costado) con arte dibujado por código.
- El usuario lo frenó: quería **organizar las ideas primero**, ver imágenes y, sobre todo, **lo quería en 3D**. Se enojó con razón porque se gastó su límite en algo que no pidió.
- Claude pidió perdón y desde ahí todo se decidió preguntando.
- El trabajo 2D **no se borró**: el usuario pidió guardarlo porque "puede estar bueno para otro tipo de juego". Está en la rama **`bichikarts-2d-referencia`** del repo, en la carpeta `bichikarts/`. Contiene:
  - Un **núcleo en C# puro**: física con gravedad por kart, reglas de agarrar, aguantar y robar, tuberías y 4 salas.
  - Una **compu (IA)** que planifica probando maniobras en una simulación chiquita.
  - **47 pruebas automáticas** que pasan (`dotnet test`).
  - **Arte 2D procedural** de bichitos, karts y salas.
  - Está **incompleto** (no abre como proyecto de Unity tal cual) y **no es este juego**.

### 3.2 Boceto 3D

Se hizo un boceto rápido en 3D (con three.js, no Unity) para mostrar la idea: cámara detrás del kart con la miguita y el aro de tiempo, un rival en la pared, otro en el techo, tuberías, terrones de azúcar, una galletita gigante y la ventana al espacio. Está en `imagenes/boceto-3d.png`. Usa nombres de bichitos viejos.

### 3.3 Rondas de preguntas

Se hicieron muchas rondas de preguntas con opciones. Todas las respuestas están en la **sección 4**.

### 3.4 El nombre

- Se pasó un texto con la descripción del juego para pedirle nombres a otra IA.
- Primero se llamó **"BichiKarts 3D"** y después quedó definitivo como **"BichiKart!"**.

### 3.5 Documento de diseño

Se escribió `DISENO.md` con todas las decisiones y se subió al repo. Se fue actualizando con lo que vino después.

### 3.6 Revisión visual con Gemini

- El usuario le pasó a Gemini el documento y el boceto. Gemini propuso:
  - URP, sombreado caricatura con contorno, formas biseladas y oclusión ambiental.
  - Luz cálida adentro y luz fría del espacio afuera.
  - Una paleta saturada tipo juguetería.
  - Descripciones de cada bichito, el kart gordito, la interfaz inflada y el menú con la casa en 3D.
- **Imagen del menú principal**: aprobada (`imagenes/menu-principal.png`).
- **Correcciones a lo que propuso Gemini:**
  - Gemini pensó los mapas como **circuitos de carrera abiertos**. Se decidió que son **salas cerradas** (piso, paredes y techo), con la ambientación de Gemini adentro.
  - Gemini propuso ruedas sin negro puro. Quedó **negro azulado**.
  - El tiempo en pantalla quedó con **barra arriba y aro** alrededor de la comida.

### 3.7 Imagen de los bichitos

- **Versión 1:** muy buena, pero con problemas:
  - **Blip se parecía a Mike Wazowski** (Monsters Inc.).
  - **Poco contraste** entre bichito y kart (Zorbo violeta en kart violeta, Mukko verde en kart verde, Klank cobre en kart naranja).
  - No se notaban las personalidades de Zorbo y Pulpi.
  - Pulpi tenía trompa de chanchito.
- **Versión 2:** corrigió todo y quedó **aprobada** (`imagenes/bichitos.png`):
  - Blip pasó a ser celeste, con orejitas.
  - Cada kart contrasta con su bichito.
  - Zorbo tiene cresta, pulsera con tachas y hace cuernitos con la mano.
  - Pulpi tiene boquita y moñito.

### 3.8 Imagen de la Cocina

- **Versión 1:** buen ambiente, pero las **tuberías eran caños de colores tipo Super Mario**. No tenían sentido en una cocina, no se entendía por dónde entrabas y salías, y tapaban la ventana. La escala tampoco era de hormiga, y la comida era una rebanada de pan entera.
- **Decisiones:**
  - Las tuberías son **caños reales de la casa**: metálicos y grises, con bocas de rejilla y una **lucecita de color** por par de bocas conectadas.
  - La comida de la Cocina es una **rebanada de pan**.
- **Versión 2:** **aprobada** como concepto principal (`imagenes/mapa-cocina.png`):
  - Caños reales con sus lucecitas.
  - Escala de hormiga: heladera gigante, salame enorme.
  - Sala cerrada: la mesada es el piso y la alacena es el techo.
- **Versión 3:** más pulida, con mejor luz y el centro despejado, pero con menos sensación de escala y menos de sala cerrada. Además el bichito del kart era una hormiga que no es del elenco. Claude recomendó la versión 2 como concepto principal.
  - Al usuario **le gustaron mucho esos gráficos** y preguntó si se podían lograr. Quedó guardada como **referencia del acabado visual buscado** (`imagenes/referencia-graficos.png`).

### 3.9 ¿Se pueden lograr esos gráficos?

- **La luz y el ambiente, sí.** Unity 6 con URP tiene luz cálida, sombras suaves, brillo, oclusión ambiental y corrección de color.
- **Los modelos detallados no los hace Claude.** Caminos:
  - Modelarlos en Blender.
  - Usar paquetes de modelos.
  - Generarlos con IA a partir de las imágenes de Gemini (por ejemplo **Meshy** o **Tripo**).
- Claude integra los modelos y se encarga de materiales, luz y código.
- El prototipo se va a ver simple. El salto visual llega en la etapa de arte (etapa 4).

### 3.10 Proyecto base de Unity

El usuario eligió hacer primero un **proyecto base** para comprobar que abre bien en su PC. Se creó y se subió a la rama `ccr-5e101442-8k3tts`, en `bichikart/BichiKart/`:
- Viene de la plantilla oficial **"3D URP"** de Unity (versión 17.1.0, basada en **Unity 6000.0.62f1 LTS**): configuración de URP para PC y celular, efectos de imagen e Input System.
- **Nombre:** BichiKart!. **Empresa:** ivanlruiz. **Identificador:** `io.github.ivanlruiz.bichikart`.
- **Paquetes:** URP 17.0.4, Input System 1.14.2, uGUI 2.0.0, Test Framework, integración con Visual Studio y Rider, y los módulos básicos.
- **Escena** `Assets/Scenes/SalaDePrueba`, con el objeto "Sala de prueba". El script `Assets/BichiKart/Scripts/Prueba/SalaDePrueba.cs` arma, al abrir la escena y sin guardarlo en ella:
  - Un rincón de la cocina con formas redondeadas: mesada de granito, pared de azulejos, ventana al espacio con marco, heladera gigante con manija, alacena como techo y pileta con canilla.
  - Las rejillas de los caños con sus lucecitas, azules y verdes.
  - El kart naranja de Blip, con Blip celeste de un ojo, orejitas y antena.
  - La rebanada de pan.
  - Luces cálidas y una luz fría del espacio.
  - En Play, el pan flota y gira, y la antena se mueve.
- `CajaRedondeada.cs` genera mallas de cajas con bordes redondeados (se verificó que las caras miran hacia afuera y que las medidas dan bien).
- El código se **compiló** contra las librerías de Unity, pero **nunca se abrió en Unity** (Claude no podía abrirlo en la nube). **Hay que probarlo.**

### 3.11 Pasar el trabajo a la PC

- El usuario bajó el repo a **`A:\Apps`** con `git clone --branch ccr-5e101442-8k3tts https://github.com/ivanlruiz/Apps.git`.
- Instaló **Claude Code** en Windows (`irm https://claude.ai/install.ps1 | iex`, versión 2.1.312, en `C:\Users\ivanc\.local\bin\claude.exe`). Tuvo que agregar esa carpeta al PATH.
- Probó `claude --teleport` y funcionó, pero no le sirvió trabajar en la terminal. Decidió **empezar una conversación nueva localmente** pasándole este archivo.

### 3.12 Primera conversación local

- **Proyecto base probado** en Unity **6000.3.14f1 (Unity 6.3 LTS)**: abre, compila sin errores y la sala de prueba se ve bien en Play.
  - Unity Hub lo abrió con esa versión porque es la única de Unity 6 instalada, y lo actualizó (por ejemplo, URP pasó a 17.3.0). Se decidió quedarse con 6000.3.14f1. El mensaje del commit `c81d2c5` dice 6000.0.62f1 por error.
  - Solo aparece un aviso informativo de URP: las sombras de las 3 luces puntuales no entran enteras en la imagen de sombras de 2048×2048 y URP les baja la calidad. Se deja para cuando se arme la luz del prototipo.
  - Al abrirlo, Unity actualizó la configuración de URP y de Shader Graph, y creó `packages-lock.json` y `.vsconfig`. Se subieron a GitHub.
- **Conversación con Gemini:** se llama "Ideas de nombres para videojuego" ("Argon" es el nombre del modelo de Gemini, no de la charla). Se leyó entera:
  - Su **revisión técnica** (kart, cámara, tuberías, compu y online) pasó a la sección **Notas técnicas** de `DISENO.md`.
  - En la parte de arte, Gemini habla de imágenes del Baño, el Cuarto de juguetes y la Nave que en realidad no generó. Siguen pendientes.
- **Decisión sobre el online:** Gemini recomendó armar el prototipo con red desde el principio. Se decidió hacerlo **sin red, pero preparado**: las órdenes de cada kart vienen de una fuente intercambiable (teclado, compu o red) y las reglas viven en un solo lugar. Después del prototipo hay una **prueba de online** antes del arte, así que las etapas pasaron a ser 5.
- **Conexión de Claude con Unity:** el usuario instaló Unity AI desde el menú **AI** del editor (paquetes `com.unity.ai.assistant` y `com.unity.ai.inference`). Con eso funciona el conector `unity-mcp`: Claude puede ver la escena y la consola, ejecutar código en el editor y sacar capturas.
- **Plan de la etapa 2:** se acordó hacerla por hitos (detalle en la sección 6).

## 4. Todas las decisiones

### Base

| Tema | Decisión |
|---|---|
| Nombre | **BichiKart!** |
| Motor | Unity 6.3 LTS (6000.3.14f1), con URP |
| Dimensión | 3D |
| Plataformas | Primero PC (teclado y mouse, también joystick); después celular |
| Tiendas | Steam, itch.io y Google Play |
| Modelo de negocio | Gratis con compras estéticas: paquetes y pase de temporada. Nunca algo que ayude a ganar |
| Mundo | Bichitos del tamaño de una hormiga dentro de una casa que flota en el espacio |
| Estilo | Caricatura redondita (tipo Fall Guys o Mario Kart). Se empieza con formas simples |
| Idiomas | Varios |
| Equipo | El usuario solo, con amigos que prueban |
| Ritmo | Sin apuro |

### Cámara y manejo

- La cámara va **detrás de tu kart**. Con el mouse se gira **una vuelta entera** y **vuelve sola** 1,5 s después de soltarlo, pero solo mientras manejás; quieto, queda donde la dejaste. El clic de la ruedita la centra.
- Cuando vas por paredes o techo, **la cámara gira con vos**. Hay opción anti-mareo.
- Las **ruedas son magnéticas**: subís manejando por las paredes hasta el techo.
- El **botón de gravedad** te tira a la superficie **opuesta**. Se recarga en **2,5 s** sin la comida y en **5 s** con la comida (adentro del caño no se recarga). Mientras volás, una marca del color de tu bichito muestra dónde vas a caer.
- El que lleva la comida recibe solo el **30 % del mini turbo**.
- Manejo **arcade con algo de peso**.
- Hay **salto**, **derrape** (con mini turbo), **freno y marcha atrás**, y **bocina**.

### Reglas

- La comida aparece en el medio. El que la agarra la lleva arriba y **va más lento**; los demás **van más rápido**.
- **Para robar alcanza con tocarlo.** El ladrón queda **protegido un segundito** y el robado **sale despedido**.
- La comida **se cae** solo si te pega un **poder** o si **dos te tocan a la vez**.
- **Esconderse está permitido.** Los perseguidores **rebotan** entre ellos.
- **Tiempo para ganar el Clásico:** 15 s con 2 jugadores y 20 s con 4. Con 3 jugadores, **20 s a confirmar**. Se puede cambiar al crear la partida.
- **Al mejor de 3** rondas.
- **Muerte súbita** a los 3 minutos: el próximo que aguante 5 segundos gana.
- Para **ubicar al dueño** hay una flecha en pantalla y un minimapa abajo a la derecha.
- El **tiempo en pantalla** se muestra con una barra inflada arriba, con la carita del dueño, y un aro alrededor de la comida.

### Tuberías

- Son **caños reales de la casa**, metálicos y grises: sifones, desagües, conductos con rejillas y mangueras. Nada de caños tipo Mario.
- Las bocas son rejillas o desagües en el piso, las paredes y el techo. Al lado de cada boca hay una **lucecita de color** igual a la de la otra punta (azul, verde, rosa o amarillo).
- Para meterse hay que **apretar F sobre la boca** (no se entra solo al pasar). Al acercarte aparece arriba de la boca un **cartel-globito** con la F, con el borde del color de la lucecita.
- **Viaje por adentro** tipo tobogán, con la cámara siguiéndote. **Te pueden seguir y robar adentro.**
- **Sonido:** succión al entrar y "plop" al salir.

### Modos

- **Clásico:** aguantar X segundos seguidos.
- **Tiempo acumulado:** gana el primero que suma **60 s**.
- **Equipos 2 contra 2:**
  - Botón de **pase** (un rival lo puede interceptar en el aire).
  - Cada uno mantiene su kart, con **banderita y aro de luz** del color del equipo.
- **Al crear la partida** se elige modo, cantidad de jugadores, mapa (versión chica o grande) y si hay poderes.
- **Al ganar:** el bichito **se come la comida**, hace su **baile** y al final hay **podio**.

### Un jugador

- **Partida rápida:** hasta **3 rivales**.
- **Copas:** 4 mapas. El primero es de calentamiento (da ventaja en la final) y después vienen 3 de eliminación.
- **Historia:**
  - En el planeta de los bichitos no hay comida y viajan a la casa espacial.
  - Un capítulo por cuarto: **Cocina → Baño → Juguetes → Nave**.
  - **Jefe final:** la **aspiradora robot**. Tiene la comida más grande y hay que robársela y aguantar mientras te persigue.
  - Lo tomamos sin jefes en cada capítulo.
- **Compu:** niveles fijos y un modo automático que se adapta.
- **Aprender:** tutorial guiado y consejos en pantalla.

### Online

| Tema | Decisión |
|---|---|
| Máximo | 4 jugadores |
| Encontrar partida | Sala con código, partida rápida, lista de salas, amigos |
| Crossplay | PC y celular juntos |
| Comunicación | Emotes y stickers, con una rueda en la tecla T |
| Si alguien se va | Lo reemplaza la compu |
| Cuentas | Steam o Google; progreso separado entre PC y celular |
| Servidores | El costo se ve después |

### Mapas

Todos son salas cerradas, con versión chica y grande:

| Mapa | Zonas | Comida |
|---|---|---|
| Cocina | Mesada y pileta, heladera abierta, alacena, mesa con mantel | **Rebanada de pan** (antes era miguita) |
| Baño | Bañadera, lavatorio, inodoro con remolino, botiquín | Caramelo |
| Cuarto de juguetes | Pista de autitos, castillo de bloques | Galletita |
| Nave espacial | Zonas sin gravedad, botón que da vuelta la sala, ventanal gigante | Comida espacial en tubo |

Elementos vivos: agua y gotas, cosas que empujan y gravedad rara (solo en la Nave).

### Poderes (opcionales)

- Salen de **cajitas** en el mapa. Cualquiera puede agarrarlas, incluso el dueño de la comida. Se guarda **uno** por vez.

| Poder | Qué hace |
|---|---|
| Turbo de azúcar | Pique de velocidad y además trepás paredes y techo más rápido |
| Manteca resbalosa | Charco de un solo uso: el primero que lo pisa patina |
| Imán | Robás desde el doble de distancia durante 4 s |
| Nube de harina | Te volvés invisible en el minimapa y en la flecha por unos segundos |

### Bichitos

- Son **6**, solo de aspecto (todos manejan igual). Tienen nombres alienígenas inventados.
- Cada uno tiene **bocina, baile, voz y reacciones propias**.

| Bichito | Aspecto | Personalidad | Kart por defecto | Disponible |
|---|---|---|---|---|
| Blip | Bolita celeste con un ojo gigante, orejitas y antena de resorte con bolita naranja | Hiperactivo | Naranja | Desde el principio |
| Pulpi | Pulpito rosa con cuatro tentáculos y moñito de concierto | Pianista | Celeste | Desde el principio |
| Zorbo | Violeta de tres ojos, cuernitos, cresta y pulsera con tachas | Rockero | Amarillo | Desde el principio |
| Mukko | Gota verde traslúcida con los ojos en dos antenitas | Pegote, cariñoso | Rosa | Se desbloquea |
| Kosmo | Astronauta diminuto con casco enorme; solo se ven dos ojitos | Explorador serio | Rojo | Se desbloquea |
| Klank | Mini robot cobre oxidado con pantallita de cara | Ex pieza de la aspiradora robot | Verde menta | Se desbloquea |

- Mukko, Kosmo y Klank se desbloquean por historia, por desafío o con monedas.
- Nombres descartados: Moco (pasó a **Mukko**), Tuerca (**Klank**), Astro (**Kosmo**). Platillo volador y alien gris quedaron afuera.

### Personalización

- **Bichito:** color; sombreros y accesorios (casco espacial, gorro de chef, colador de pasta, corona, anteojos); ojos y bocas.
- **Kart:**
  - Colores, con ruedas siempre **negro azulado**.
  - Calcos: números y llamas, comida, espaciales y caras de bichitos.
  - Detalles: alerón y bocina.
  - Estelas: burbujas y estrellitas.

### Progreso

- **Monedas** para lo común.
- **Desafíos** para lo especial: permanentes, diarios, semanales y de temporada. Son de robo, de aguante, de gravedad y tuberías, y de colección.
- **Niveles** hasta el **50**. Dan monedas, accesorios o calcos, marcos y títulos.
- **Temporadas** de 2 a 3 meses: Navidad en órbita, Halloween, Verano y Lluvia de meteoritos.
- **Pase de temporada** con camino gratis y premium.
- **Eventos:** modos locos, doble monedas e invasión de la aspiradora.

### Controles de PC

| Tecla | Acción |
|---|---|
| W | Acelerar |
| S | Frenar y marcha atrás |
| A / D | Doblar |
| Espacio | Saltar |
| Shift | Derrapar |
| Ctrl | Gravedad |
| E | Poder |
| F | Usar (meterse en un caño) |
| Q | Bocina |
| R | Pasar la comida |
| T | Rueda de emotes y stickers |
| Mouse | Girar la cámara |
| Clic de la ruedita | Centrar la cámara |

- **Emotes:** reírse, enojarse, saludar y llorar.
- **Stickers:** "¡Vení acá!", "¡Te la robo!", "¡Buena!" y "¡Ups!".
- **Celular** (más adelante): palanca virtual a la izquierda; botones de acelerar, saltar y gravedad a la derecha; deslizar el dedo mueve la cámara.

### Interfaz

- **Menú principal:** la casa vista desde afuera, flotando en el espacio. Cada cuarto es un modo:
  - Cocina: historia.
  - Living: partida rápida.
  - Garaje: personalizar.
  - Antena: online.
  - Ventana con trofeos: copas.
  - Botones de Opciones y Salir.
- **Repeticiones:** al final de la partida, con cámaras libres y cámara lenta, y se pueden guardar.
- **Accesibilidad:** anti-mareo, modo para daltonismo, teclas a gusto y subtítulos.

### Sonido

- **Música:** funk espacial. Se pone tensa en los últimos segundos y suma un instrumento cuando tenés la comida.
- **Voces:** idioma inventado con balbuceo suave.
- **Robo:** "plop" para el ladrón y grito ofendido del robado.

### Arte (de la revisión con Gemini)

- **Técnica:** URP, sombreado caricatura con contorno fino, formas biseladas (ProBuilder para el prototipo), luz cálida adentro y fría del espacio afuera, y oclusión ambiental.
- **Paleta:** colores saturados con base pastel, sin negros puros; las sombras van en azul noche o violeta.
- **Kart:** chasis gordito de plástico, paragolpes redondeado, motorcito y escapes grandes atrás, y alerón de goma.

## 5. Estado técnico actual

| Qué | Dónde |
|---|---|
| Repo | `github.com/ivanlruiz/Apps`, bajado en **`A:\Apps`** |
| Rama de trabajo | **`ccr-5e101442-8k3tts`** |
| Versión de Unity | **6000.3.14f1** (Unity 6.3 LTS) |
| Conexión con Claude | Unity AI (`com.unity.ai.assistant`) y el conector `unity-mcp` |
| Documento de diseño | `bichikart/DISENO.md` |
| Este historial | `bichikart/HISTORIAL.md` |
| Instrucciones del proyecto | `bichikart/README.md` |
| Imágenes | `bichikart/imagenes/`: `boceto-3d.png`, `menu-principal.png`, `bichitos.png`, `mapa-cocina.png` (concepto aprobado), `referencia-graficos.png` (acabado buscado) |
| Proyecto de Unity | `bichikart/BichiKart/` (proyecto base, probado en Unity: abre y se ve bien) |
| Escenas | `SalaDePrueba` (la sala del proyecto base) y `Prototipo` (donde se arma la etapa 2) |
| Código | `Assets/BichiKart/Scripts` (juego), `Assets/BichiKart/Editor` (herramientas del editor) y `Assets/BichiKart/Pruebas` (pruebas automáticas) |
| Pruebas automáticas | En Unity, menú **BichiKart → Correr pruebas**; el resultado sale en la consola |
| Prototipo 2D viejo | Rama `bichikarts-2d-referencia`, carpeta `bichikarts/` |

## 6. Etapas y próximos pasos

1. ~~Documento de diseño~~ (hecho).
2. **Prototipo jugable contra la compu** con formas simples. Sin online, poderes ni arte final, pero con el código preparado para la red (ver **Notas técnicas** en `DISENO.md`). Se hace por hitos: Claude los avanza solo, los prueba en Unity y le pregunta al usuario solo lo de diseño.
   - **Hito 0 (hecho). Preparar el proyecto:** conexión con Unity, versión corregida en los documentos, paquetes (Cinemachine 3.1.7, Splines 2.8.4 e Input System 1.19.0), assembly definitions y escena `Prototipo`.
   - **Hito 1 (hecho). La sala y el kart en el piso:** la Cocina como caja cerrada con los rincones redondeados por dentro, 2 o 3 obstáculos, el kart (una esfera física invisible con el kart de Blip encima), W/S/A/D y la cámara detrás, que gira con el mouse y vuelve sola. Ver la sección 9.
   - **Hito 2 (hecho). Paredes, techo y gravedad:** ruedas magnéticas, cámara que gira con vos (con anti-mareo) y Ctrl.
   - **Hito 3 (hecho). Salto, derrape y bocina.**
   - **Hito 4 (hecho). El pan y las reglas:** el árbitro de la partida con pruebas automáticas, la interfaz (barra, aro, flecha y minimapa) y 3 rivales de práctica.
   - **Hito 5 (hecho). La compu de verdad:** los rivales usan paredes, techo y gravedad, el caño, derrapan en las curvas y se esconden cuando tienen el pan. Sus números están en `AjustesDeCompu`.
   - **Hito 6 (hecho, antes que el 5). La tubería:** un caño hecho con Splines entre un desagüe del piso y una rejilla del techo. Se hizo antes que la compu para que la compu pueda aprender a usarlo.
   - **Hito 7 (en curso). Ajuste final.** El usuario jugó y contestó: la velocidad está bien; era **muy fácil** porque con la gravedad nadie lo alcanzaba ("pondría un cooldown"); la cámara no giraba entera con el mouse ("la haría 360"); y el caño tendría que tener un **cartel** y entrarse **apretando una tecla**, no solo pasando. Se le preguntó también a Argon (Gemini). Primera vuelta de ajustes, con lo que eligió el usuario:
     - **Gravedad con espera** (la propuesta de Argon): se recarga en 2,5 s sin el pan y en 5 s con el pan; adentro del caño no se recarga. Mientras volás hay una marca del color del bichito donde vas a caer, y abajo a la izquierda un aro que se llena y late cuando está lista (amarillo si llevás el pan).
     - **Caño con F** (Argon la recomendó porque E queda para los poderes): cartel-globito arriba de cada boca con la F, chiquito desde 8 de distancia y grande y saltando cuando ya se puede entrar (a menos de 2,6 de la boca). Se puede llegar con la F ya apretada. Joystick: X.
     - **Cámara 360°**: más sensible (0,25° por punto en vez de 0,12°); vuelve sola a los 1,5 s solo mientras manejás; clic de la ruedita (o stick derecho apretado) para centrarla.
     - **Extras:** el que lleva el pan recibe el 30 % del mini turbo, y la compu tarda **0,5 a 0,8 s** en reaccionar cuando el kart que mira usa la gravedad.
     - Quedó sin aclarar si "Caño o choques" se refería a algo más que la entrada automática.
   - **Decisiones:**
     - La sala tiene **medidas ajustables**, y arranca con un tamaño para 4 karts (el diseño dice que la versión chica es para 2).
     - Además de las reglas básicas, entran el **rebote entre perseguidores**, la **caída por doble toque**, el **mejor de 3 rondas** y la **muerte súbita**.
     - El **minimapa** es una vista de arriba: el ícono de cada kart cambia según esté en el piso, en una pared o en el techo.
     - Cada hito se cierra con commit y push cuando Claude lo probó en Unity. El usuario lo prueba cuando puede.
     - Después del hito 6 (respuestas del usuario): al que se le cae el pan no lo puede volver a agarrar durante **0,6 s**; la flecha apunta también al pan suelto; la cámara va **un poco más alta** para ver por encima del pan; la compu usa **a propósito** paredes, techo, gravedad y el caño, y **se esconde** detrás del terrón o del frasco cuando tiene el pan. La velocidad (16) queda como está hasta que el usuario la pruebe.
3. **Prueba de online**: dos PCs en la misma sala, con la comida y el robo, antes de invertir en arte.
4. Bichitos, mapas y arte (modelos 3D).
5. Online completo y partidas personalizadas.

~~**Lo primero:** comprobar que el proyecto base abre bien en Unity~~ (hecho: abre, compila y la sala se ve bien en Play).

**Ahora:** hito 7 de la etapa 2, el ajuste final (los hitos 0 a 6 están hechos). Ya está la primera vuelta de ajustes (gravedad con espera, caño con F, cámara 360°); falta que el usuario la juegue y cuente si ahora es difícil pero justo.

## 7. Pendientes del diseño

- Confirmar el tiempo para ganar con 3 jugadores (por ahora 20 s).
- Imágenes de concepto del Baño, el Cuarto de juguetes, la Nave y el kart para personalizar. Ya hay prompts preparados: piden caños reales, escala de hormiga, sala cerrada y centro despejado.
- Botones del joystick y detalle de los controles de celular.
- Lista concreta de desafíos y sus premios.
- Contenido paso a paso del tutorial.
- Nombres de las copas y qué mapas tiene cada una.
- Mapas, bichitos y comidas de cada temporada.
- Lista completa de accesorios, calcos y estelas, y cuáles están disponibles al empezar.
- Precios: monedas por partida, tienda y paquetes.
- Librería de red para el online (Netcode for GameObjects o FishNet), servicio de servidores y su costo.

## 8. Lecciones de esta conversación

- **No arrancar a programar sin preguntar.** El primer intento en 2D se hizo sin consultar y no era lo que el usuario quería.
- **Mostrar imágenes** ayuda mucho a decidir.
- Revisar las propuestas de otras IA **contra lo ya decidido**. Gemini propuso circuitos abiertos y caños tipo Mario, y hubo que corregirlo.
- Cuidar que los diseños **no se parezcan a personajes o juegos conocidos** (Mike Wazowski, los caños de Mario).

## 9. El prototipo (etapa 2)

### Cómo probarlo

1. En Unity, abrir la escena `Assets/Scenes/Prototipo` y apretar **Play**.
2. Hacer **clic** en la ventana del juego para que el mouse mueva la cámara. **Escape** suelta el mouse.
3. Controles:

| Tecla | Acción | Joystick |
|---|---|---|
| W / S o flechas | Acelerar / frenar y marcha atrás | Gatillo derecho / izquierdo |
| A / D o flechas | Doblar | Stick izquierdo |
| Espacio | Saltar | A |
| Shift (mantener, doblando) | Derrapar; al soltar con la carga completa sale el mini turbo | RB |
| Ctrl | Gravedad: te tira a la superficie de enfrente (se recarga en 2,5 s, o 5 s con el pan) | Y |
| F (sobre una boca) | Meterse en el caño; se puede mantener apretada mientras llegás | X |
| Q | Bocina | LB |
| Mouse | Girar la cámara (vuelta entera) | Stick derecho |
| Clic de la ruedita | Centrar la cámara | Stick derecho apretado |

Para pasar a las paredes y al techo, alcanza con manejar hacia un rincón: las ruedas son magnéticas.

La partida arranca sola: cuenta regresiva, la rebanada de pan en el medio y 3 rivales de práctica (Pulpi, Zorbo y Mukko). En pantalla:

- **Arriba**, la barra inflada con la carita del dueño avanzando hacia la meta, y debajo cuánto le falta.
- **En el medio**, la cuenta regresiva y los avisos ("¡Pulpi se lo robó a Blip!").
- **Alrededor del pan**, el aro de tiempo, que se va prendiendo con el color del dueño.
- **Una flecha** que apunta al dueño (o al pan, si está suelto): flota arriba de él si se ve, o queda en el borde de la pantalla si no.
- **Abajo a la derecha**, el minimapa holográfico visto desde arriba: cada kart es un círculo de su color, lleno en el piso, anillo en el techo y las dos cosas (más chico) en una pared. Blip tiene una flechita de rumbo y el pan titila.
- **Arriba a la izquierda**, el marcador de rondas.
- **Abajo a la izquierda**, el botón de gravedad: un aro que se llena mientras se recarga (amarillo si llevás el pan) y da un saltito cuando está listo.
- **Arriba de cada boca del caño**, un cartel-globito con la F: chiquito cuando andás cerca y grande, saltando, cuando ya podés meterte.
- **Donde vas a caer** después de usar la gravedad, un aro del color del bichito (también se ve el de los rivales).

Si algo de la escena se rompe, el menú **BichiKart → Armar escena Prototipo** la vuelve a armar de cero.

### Valores elegidos (se pueden cambiar mientras se juega)

Están en `Assets/BichiKart/Ajustes`: se elige el archivo en Unity y se cambian los números en el Inspector.

| Archivo | Qué tiene | Valores de arranque |
|---|---|---|
| `AjustesDeSala` | Medidas de la Cocina y radio de los rincones | 40 × 14 × 32, rincones de 3,5 |
| `AjustesDelKart` | Velocidad, aceleración, frenado, giro, agarre, gravedad, salto, derrape y turbo | Máxima 16, llega en ~1 s, marcha atrás 7, dobla 150° por segundo. La gravedad se recarga en 2,5 s (Espera Gravedad) y en 5 s con el pan (Espera Gravedad Con Comida). El mini turbo se carga con 0,9 s de derrape y lleva a 21,6 durante 0,8 s; con el pan empuja el 30 % (Turbo Con Comida) |
| `ReglasDePartida` | Tiempo para ganar, rondas, muerte súbita, protección, doble toque, velocidades con y sin el pan, empujones y pausas | 15/20/20 s según jugadores, al mejor de 3, muerte súbita a los 3 min (5 s), protección 1 s, doble toque 0,1 s (y 0,6 s sin poder volver a agarrarlo), dueño ×0,85 y perseguidores ×1,10, cuenta de 3 s |
| `AjustesDeCompu` | Cómo juega la compu: reacción, giro, anticipación, derrape, gravedad, caño, esconderse y huir | Decide cada 0,25 s, tarda 0,5 a 0,8 s en reaccionar cuando el que mira usa la gravedad (Reaccion A Gravedad), se tira con la gravedad si el objetivo está a menos de 8 de costado en la superficie de enfrente (cuando el botón de su kart está recargado), busca el caño a menos de 10 de una boca y aprieta F al llegar, huye si alguien se acerca a menos de 9 |

La cámara va a 6,5 detrás del kart y gira con él en las paredes y el techo. Con el mouse da la vuelta entera (Sensibilidad Mouse 0,25). Vuelve sola atrás 1,5 s después de soltar el mouse, pero solo mientras manejás, y el clic de la ruedita la centra. En el objeto **Cámara del kart** se puede tildar **Anti Mareo** para que acompañe más despacio los giros. En el objeto **Tubería azul**, **Radio Boca** (2,6) es la distancia para meterse con F y **Distancia Cartel** (8) desde dónde se ve el cartel.

### Qué probar para el ajuste final (hito 7)

Cada punto dice qué valor tocar si algo no se siente bien. Todos están en `Assets/BichiKart/Ajustes` (se eligen en Unity y se cambian en el Inspector, también mientras se juega), salvo la cámara, que está en el objeto **Cámara del kart**.

| Qué probar | Si no convence, tocar |
|---|---|
| ¿La velocidad (16) es buena para esta sala, o muy rápida o lenta? | `AjustesDelKart` → Velocidad Maxima, Aceleracion |
| ¿Doblar y derrapar se siente bien? ¿El mini turbo se nota? | `AjustesDelKart` → Giro, Giro Derrape, Carga Mini Turbo, Velocidad Turbo |
| ¿Subir por las paredes y andar por el techo marea? | Cámara del kart → Anti Mareo |
| ¿Sirve el botón de gravedad? ¿Se puede abusar? ¿Con el pan ahora te alcanzan? | `AjustesDelKart` → Impulso Gravedad, Espera Gravedad, Espera Gravedad Con Comida |
| ¿20 s con el pan contra 3 compus es posible? | `ReglasDePartida` → Tiempo Cuatro Jugadores, Velocidad Duenio, Velocidad Perseguidores |
| ¿Se nota cuando te roban? ¿El segundito de protección alcanza? | `ReglasDePartida` → Empuje Robado, Salto Robado, Proteccion Tras Robo |
| ¿Se entiende el caño (por dónde entrar y salir)? ¿Se ve el cartel a tiempo? ¿Dura bien el viaje? | Objeto **Tubería azul** → Velocidad, Radio Boca, Distancia Cartel |
| ¿La cámara gira cómoda y vuelve cuando tiene que volver? | Cámara del kart → Sensibilidad Mouse; la espera de 1,5 s está en el eje Horizontal de la órbita (Recentering → Wait) |
| ¿La compu es muy buena o muy mala? | `AjustesDeCompu` → Reaccion, Anticipacion, Usa Gravedad, Usa Canio |
| ¿Se leen la barra, la flecha, el aro y el minimapa? | Contarle a Claude qué no se entiende |
| ¿La sala es chica o grande para 4? | `AjustesDeSala` → Medidas |

### Qué hay en la escena

- **La Cocina:** sala cerrada con rincones redondeados. El piso es la mesada de granito, el techo la alacena de madera, un costado la heladera y el resto azulejos, con la ventana al espacio. Tiene una pileta plana, un **terrón de azúcar** y un **frasco de mermelada** para esconderse, migas de adorno y 4 luces cálidas y frías (solo una con sombra).
- **El kart de Blip:** una esfera física invisible con el kart encima. Las ruedas giran con la velocidad, las de adelante doblan y la antena se bambolea. Al derrapar se cruza y le salen chispas celestes, que se ponen naranjas con el mini turbo cargado; con el turbo, los escapes tiran fuego.
- **Ruedas magnéticas:** apoyado, la gravedad tira hacia la superficie que el kart tiene abajo. Al pasar por un rincón redondeado, sube por la pared y sigue hasta el techo.
- **Botón de gravedad:** solo apoyado y recargado (2,5 s sin el pan, 5 s con el pan; la recarga va según quién lleva el pan en cada momento y se frena adentro del caño). El kart despega hacia la superficie de enfrente, gira como un tonel en el aire y aterriza sin perder velocidad. Mientras vuela, un aro de su color marca dónde va a caer (calculado con la velocidad y la gravedad, se corrige si dobla en el aire).
- **Sonidos generados por código:** bocina, despegue de la gravedad, salto y mini turbo; "plop" al agarrar el pan y queja al perderlo.
- **La partida:** un **árbitro** (C# puro, con pruebas automáticas) lleva todas las reglas: cuenta regresiva, agarrar, aguantar, robar con un toque, protección de 1 s, caída por doble toque, más lento con el pan y más rápido sin él, al mejor de 3 y muerte súbita. La escena le cuenta los toques y aplica lo que pasa: el robado sale despedido y los perseguidores rebotan entre ellos. Los toques entre karts se miden como entre cápsulas (la forma alargada del kart).
- **La compu (Pulpi, Zorbo y Mukko):** busca el pan suelto; persigue al dueño adelantándose a donde va a estar; si el dueño está en la superficie de enfrente, más o menos encima, usa la gravedad; si está cerca de la otra boca del caño, se mete por el caño (aprieta F sobre la boca, como el jugador). Cuando el que mira usa la gravedad, tarda entre 0,5 y 0,8 s en reaccionar, como una persona. Con el pan, se esconde detrás del terrón o del frasco, y si alguien se acerca escapa (con la gravedad o el caño si lo tienen encima). Esquiva los obstáculos, derrapa en las curvas cerradas para el mini turbo y, si se traba, retrocede. Por ahora todos tienen la forma de Blip, con sus colores. El rival simple del hito 4 (`FuentePractica`) quedó en el proyecto sin usar; puede servir para un nivel "Fácil".
- **La tubería azul:** un caño de metal que une un desagüe del piso (cerca de la heladera, adelante) con una rejilla del techo en la esquina de enfrente. Va por afuera de la sala: baja, pasa por debajo del piso, sube por detrás de la heladera y cruza por arriba del techo. Las dos bocas tienen una lucecita azul y un cartel-globito con la F que aparece cuando el jugador se acerca. Para meterse hay que apretar F a menos de 2,6 de la boca (pasar por encima ya no alcanza); adentro se viaja solo a 24 (unos 3 segundos), la cámara va pegada atrás, doblar hace girar al kart alrededor del caño y se puede robar si otro te alcanza. Se sale por la otra boca, ya pegado a esa superficie (por ejemplo, al techo). Hay succión al entrar y "plop" al salir.

### Lecciones del hito 1

- Se probó en Play con un **piloto automático** que maneja solo y anota posición y velocidad, a través de la conexión con Unity.
- Las salidas de los karts tienen que dejar lugar atrás para la cámara: si no, la cámara choca con la pared y queda pegada al kart.

### Lecciones de los hitos 2 y 3

- **Derrape:** si la velocidad no gira con el kart, el derrape frena de golpe (de 16 a 4) y nunca carga el turbo. Quedó así: el kart dobla más cerrado y se desliza un poco hacia afuera de la curva, sin perder velocidad.
- **Obstáculos:** van en las diagonales, para que no tapen el centro ni las salidas.
- **Revisión con un agente:** encontró que las texturas se amontonaban en los rincones redondeados (corregido, con pruebas), que el juego compilado no iba a tener los shaders (ahora hay materiales base en `Assets/BichiKart/Resources/Formas`) y otros detalles menores.

### Lecciones del hito 4

- **Unity en segundo plano:** si la ventana de Unity no está al frente, el juego en Play no avanza. Se activó **Run In Background** (también hace falta para el online), y para probar sin traer Unity al frente Claude pausa el juego y lo avanza cuadro por cuadro.
- Lo que se cambia en un archivo de ajustes **durante Play queda guardado** al salir: para probar con otros valores hay que acordarse de volverlos atrás.

### Lecciones del hito 5

- En una simulación de un minuto con 4 compus hubo **21 robos**: nadie aguantó más de 5 segundos seguidos. Cada compu usó la gravedad unas 11 veces (se subió la espera entre usos a 4 s) y el caño entre 1 y 3 veces. Aguantar 20 s contra 3 compus puede ser muy difícil: hay que ver jugando si conviene que la compu reaccione más lento o que la diferencia de velocidad sea menor.

### Lecciones del hito 6

- La interfaz va **encima de todo** (Screen Space Overlay): dibujada a una distancia de la cámara, la tapaba la pared del caño. Para las capturas, Claude la pasa un momento al modo de cámara.

### Lecciones del hito 7 (primera vuelta)

- Se probó en Play con un piloto automático: sin el pan, la gravedad salió 3 veces en 6,2 s (cada 2,50 s); con el pan, cada 5 s; la marca de caída se vio en cada vuelo y el aro de la interfaz coincidió con la carga (0,49).
- Sobre la boca **sin F** el kart no se mete; con F entra enseguida y sale por el techo. El cartel se ve chiquito (0,6) a 6 de la boca y grande (1) encima.
- La compu con el pan escapó por el caño apretando F (tardó 2,6 s porque arrancó mirando para el otro lado). Duda entre "escapar por el caño" y "esconderse" cuando el perseguidor queda justo a 9: es algo para mirar si se nota jugando.
- Cámara: girada 400° quedó en 40° (da la vuelta entera); quieta no volvió; manejando volvió al centro entre 1,5 y 2,5 s.
- La vuelta automática de la cámara (de Cinemachine) no sabe si estás manejando: para que no vuelva estando quieto, `CamaraDelKart` le reinicia la espera en cada cuadro en que no se acelera ni se dobla.
- En las fotos, lo que brilla mucho se ve blanco por el resplandor (bloom): el cartel y la marca usan un brillo bajo (1,2) para que se note su color.
