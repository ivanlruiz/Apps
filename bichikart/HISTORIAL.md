# BichiKart! — Historial completo de la conversación

Este archivo resume **todo** lo que se habló en la primera conversación sobre el juego, de principio a fin, para poder seguir en una conversación nueva sin perder nada.

- El **diseño ordenado** está en [`DISENO.md`](DISENO.md). Si algo de este historial choca con `DISENO.md`, vale lo último que se decidió, que está marcado acá.
- Las **imágenes** están en [`imagenes/`](imagenes/).

---

## 1. Cómo quiere trabajar el usuario (importante)

- Hablarle en **español rioplatense** (voseo).
- **Preguntar TODO antes de hacer algo.** No programar, crear ni cambiar cosas sin aprobación. Antes de cada etapa, decir exactamente qué se va a hacer y esperar el sí.
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
- El prototipo se va a ver simple. El salto visual llega en la etapa 3.

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

## 4. Todas las decisiones

### Base

| Tema | Decisión |
|---|---|
| Nombre | **BichiKart!** |
| Motor | Unity 6 (URP) |
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

- La cámara va **detrás de tu kart**. Con el mouse se gira y **vuelve sola** al soltarlo.
- Cuando vas por paredes o techo, **la cámara gira con vos**. Hay opción anti-mareo.
- Las **ruedas son magnéticas**: subís manejando por las paredes hasta el techo.
- El **botón de gravedad** te tira a la superficie **opuesta**.
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
| Q | Bocina |
| R | Pasar la comida |
| T | Rueda de emotes y stickers |
| Mouse | Girar la cámara |

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
| Documento de diseño | `bichikart/DISENO.md` |
| Este historial | `bichikart/HISTORIAL.md` |
| Instrucciones del proyecto | `bichikart/README.md` |
| Imágenes | `bichikart/imagenes/`: `boceto-3d.png`, `menu-principal.png`, `bichitos.png`, `mapa-cocina.png` (concepto aprobado), `referencia-graficos.png` (acabado buscado) |
| Proyecto de Unity | `bichikart/BichiKart/` (proyecto base, sin probar en Unity todavía) |
| Prototipo 2D viejo | Rama `bichikarts-2d-referencia`, carpeta `bichikarts/` |

## 6. Etapas y próximos pasos

1. ~~Documento de diseño~~ (hecho).
2. **Prototipo jugable contra la compu** con formas simples. **No empezar sin que el usuario lo confirme.** Plan propuesto:
   - La Cocina en versión chica, como sala cerrada.
   - Tu kart con:
     - Cámara detrás que gira con vos y con el mouse.
     - Ruedas que se pegan a paredes y techo.
     - Ctrl para la gravedad.
     - Salto, derrape, freno con marcha atrás y bocina.
   - La rebanada de pan: agarrar, aguantar 20 s y robar con un toque (protección de un segundito y el robado sale despedido).
   - Barra de tiempo, aro, flecha al dueño y minimapa.
   - 3 rivales de la compu.
   - Una tubería con viaje por adentro.
   - Sin online, poderes ni arte final.
3. Bichitos, mapas y arte (modelos 3D).
4. Online y partidas personalizadas.

**Lo primero ahora:** comprobar que el proyecto base abre bien en Unity:
1. En Unity Hub: **Add → Add project from disk** → `A:\Apps\bichikart\BichiKart`.
2. Abrir `Assets/Scenes/SalaDePrueba` y apretar **Play**.
3. Si aparece algún error en la consola, corregirlo.

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
- Servicio de servidores para el online (Unity Gaming Services o Photon) y su costo.

## 8. Lecciones de esta conversación

- **No arrancar a programar sin preguntar.** El primer intento en 2D se hizo sin consultar y no era lo que el usuario quería.
- **Mostrar imágenes** ayuda mucho a decidir.
- Revisar las propuestas de otras IA **contra lo ya decidido**. Gemini propuso circuitos abiertos y caños tipo Mario, y hubo que corregirlo.
- Cuidar que los diseños **no se parezcan a personajes o juegos conocidos** (Mike Wazowski, los caños de Mario).
