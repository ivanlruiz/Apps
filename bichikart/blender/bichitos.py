"""
Los seis bichitos de BichiKart! hechos con formas simples y suaves, según el concepto
(imagenes/bichitos.png) y la tabla de arte de DISENO.md. Saca un render de los seis en fila y
exporta cada uno a .fbx para Unity.

Uso, desde la carpeta bichikart:
  "A:\\Blender\\blender.exe" --background --factory-startup --python blender/bichitos.py --
      BichiKart/Assets/BichiKart/Resources/Bichitos imagenes/bichitos-3d.png
(el primero es la carpeta de los modelos y el segundo, el render; si falta, va junto a los modelos).

Medidas: 1 unidad = 1 metro de Unity. Cada bichito mide ~1,2 de alto, con la base en z = 0 y
mirando hacia -Y (hacia la cámara). El nombre de cada material lleva "#color|aspereza|brillo|laca"
(en el FBX las "|" quedan como "_"), que el juego lee para armarlo con sus propios materiales.
"""
import math
import os
import sys

import bpy
from mathutils import Vector

argumentos = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
salida = os.path.abspath(argumentos[0]) if argumentos else os.getcwd()
archivo_render = os.path.abspath(argumentos[1]) if len(argumentos) > 1 else os.path.join(salida, "bichitos.png")
os.makedirs(salida, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
escena = bpy.context.scene
_materiales = {}


# ---------------------------------------------------------------- materiales

def lineal(hexa):
    hexa = hexa.lstrip("#")

    def canal(c):
        c = int(c, 16) / 255
        return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4

    return (canal(hexa[0:2]), canal(hexa[2:4]), canal(hexa[4:6]), 1.0)


def mat(hexa, aspereza=0.45, brillo=0.0, vidrio=0.0, laca=0.0):
    """
    Material liso; se reusa si ya hay uno igual. El nombre lleva "#color|aspereza|brillo": Unity lo lee
    para armar el bichito con los mismos materiales del juego (y así también se puede desvanecer).
    """
    clave = (hexa, aspereza, brillo, vidrio, laca)
    if clave in _materiales:
        return _materiales[clave]
    m = bpy.data.materials.new(f"{hexa}|{aspereza:.2f}|{brillo:.1f}|{laca:.1f}")
    m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = lineal(hexa)
    bsdf.inputs["Roughness"].default_value = aspereza
    if brillo > 0:
        bsdf.inputs["Emission Color"].default_value = lineal(hexa)
        bsdf.inputs["Emission Strength"].default_value = brillo
    if vidrio > 0:
        bsdf.inputs["Transmission Weight"].default_value = vidrio
    if laca > 0:
        bsdf.inputs["Coat Weight"].default_value = laca
    _materiales[clave] = m
    return m


# ---------------------------------------------------------------- formas

class Bichito:
    """Un bichito: un objeto vacío en el origen con todas sus partes colgando de él."""

    def __init__(self, nombre):
        self.nombre = nombre
        self.raiz = bpy.data.objects.new(nombre, None)
        bpy.context.collection.objects.link(self.raiz)

    def _colgar(self, obj, nombre, material):
        obj.name = f"{self.nombre} {nombre}"
        obj.parent = self.raiz
        if material is not None:
            obj.data.materials.append(material)
        return obj

    def esfera(self, nombre, lugar, escala, material, giro=(0, 0, 0), suave=1):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=1, location=lugar,
                                             rotation=[math.radians(a) for a in giro])
        obj = bpy.context.active_object
        obj.scale = escala
        bpy.ops.object.shade_smooth()
        if suave:
            # El suavizado extra solo va en el render: al juego va sin él, que es más liviano.
            m = obj.modifiers.new("Suave", "SUBSURF")
            m.levels, m.render_levels = 0, suave + 1
        return self._colgar(obj, nombre, material)

    def gota(self, nombre, lugar, radio, alto, material):
        """Una gota: redonda abajo y finita arriba."""
        bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=1, location=lugar)
        obj = bpy.context.active_object
        for v in obj.data.vertices:
            t = (v.co.z + 1) / 2
            if t > 0.45:
                achicar = 1 - 0.8 * ((t - 0.45) / 0.55) ** 1.3
                v.co.x *= achicar
                v.co.y *= achicar
        obj.scale = (radio, radio * 0.92, alto)
        bpy.ops.object.shade_smooth()
        m = obj.modifiers.new("Suave", "SUBSURF")
        m.levels, m.render_levels = 0, 2
        return self._colgar(obj, nombre, material)

    def cono(self, nombre, lugar, radio, alto, material, giro=(0, 0, 0)):
        bpy.ops.mesh.primitive_cone_add(vertices=24, radius1=radio, radius2=radio * 0.15, depth=alto, location=lugar,
                                        rotation=[math.radians(a) for a in giro])
        obj = bpy.context.active_object
        bpy.ops.object.shade_smooth()
        m = obj.modifiers.new("Suave", "SUBSURF")
        m.levels, m.render_levels = 0, 2
        return self._colgar(obj, nombre, material)

    def cilindro(self, nombre, lugar, radio, alto, material, giro=(0, 0, 0)):
        bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=radio, depth=alto, location=lugar,
                                            rotation=[math.radians(a) for a in giro])
        obj = bpy.context.active_object
        bpy.ops.object.shade_smooth()
        b = obj.modifiers.new("Borde", "BEVEL")
        b.width, b.segments = min(radio, alto) * 0.3, 3
        return self._colgar(obj, nombre, material)

    def caja(self, nombre, lugar, tamano, radio, material, giro=(0, 0, 0)):
        """Caja con las puntas redondeadas."""
        bpy.ops.mesh.primitive_cube_add(size=1, location=lugar, rotation=[math.radians(a) for a in giro])
        obj = bpy.context.active_object
        obj.scale = tamano
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        b = obj.modifiers.new("Borde", "BEVEL")
        b.width, b.segments, b.limit_method = radio, 5, "NONE"
        bpy.ops.object.shade_smooth()
        try:
            bpy.ops.object.shade_auto_smooth(angle=math.radians(35))
        except Exception:
            pass
        return self._colgar(obj, nombre, material)

    def tubo(self, nombre, puntos, grosor, material, radios=None):
        """Un tubo suave que pasa por los puntos (tentáculos, antenas, sonrisas). Se angosta según radios."""
        curva = bpy.data.curves.new(nombre, "CURVE")
        curva.dimensions = "3D"
        curva.bevel_depth = grosor
        curva.bevel_resolution = 4
        curva.use_fill_caps = True
        spline = curva.splines.new("BEZIER")
        spline.bezier_points.add(len(puntos) - 1)
        for i, (p, punto) in enumerate(zip(puntos, spline.bezier_points)):
            punto.co = p
            punto.handle_left_type = punto.handle_right_type = "AUTO"
            punto.radius = radios[i] if radios else 1.0
        obj = bpy.data.objects.new(nombre, curva)
        bpy.context.collection.objects.link(obj)
        return self._colgar(obj, nombre, material)

    def resorte(self, nombre, base, alto, radio, vueltas, grosor, material):
        pasos = vueltas * 12
        puntos = []
        for i in range(pasos + 1):
            a = i / pasos * vueltas * 2 * math.pi
            puntos.append((base[0] + math.cos(a) * radio, base[1] + math.sin(a) * radio, base[2] + alto * i / pasos))
        curva = bpy.data.curves.new(nombre, "CURVE")
        curva.dimensions = "3D"
        curva.bevel_depth = grosor
        curva.bevel_resolution = 3
        spline = curva.splines.new("NURBS")
        spline.points.add(len(puntos) - 1)
        for p, punto in zip(puntos, spline.points):
            punto.co = (*p, 1)
        spline.use_endpoint_u = True
        spline.order_u = 4
        obj = bpy.data.objects.new(nombre, curva)
        bpy.context.collection.objects.link(obj)
        return self._colgar(obj, nombre, material)

    def ojo(self, lugar, radio, iris_hexa, mirada=(0, 0), grande=False):
        """Ojo blanco con iris, pupila y brillito, mirando hacia -Y."""
        x, y, z = lugar
        self.esfera("Ojo", (x, y, z), (radio, radio * 0.6, radio * 1.05), mat("#ffffff", 0.15))
        dx, dz = mirada
        iris = radio * (0.62 if grande else 0.55)
        self.esfera("Iris", (x + dx * radio, y - radio * 0.5, z + dz * radio), (iris, iris * 0.35, iris * 1.05), mat(iris_hexa, 0.2))
        pupila = iris * 0.55
        self.esfera("Pupila", (x + dx * radio, y - radio * 0.6, z + dz * radio), (pupila, pupila * 0.35, pupila), mat("#0d1026", 0.1))
        brillo = radio * 0.18
        self.esfera("Brillito", (x + dx * radio + iris * 0.35, y - radio * 0.66, z + dz * radio + iris * 0.4), (brillo, brillo * 0.5, brillo),
                    mat("#ffffff", 0.1, 4.0), suave=0)

    def brazos(self, color, alto, ancho, adelante=-0.22, grosor=0.065):
        """Bracitos para adelante, como agarrando un volante."""
        for lado in (-1, 1):
            self.esfera("Brazo", (lado * ancho, adelante * 0.5, alto), (grosor, 0.15, grosor), color, giro=(25, 0, 0))
            self.esfera("Mano", (lado * (ancho - 0.06), adelante - 0.08, alto - 0.07), (grosor * 1.25,) * 3, color)

    def patas(self, color, separacion=0.16):
        for lado in (-1, 1):
            self.esfera("Pata", (lado * separacion, -0.08, 0.05), (0.1, 0.14, 0.055), color)


def sonrisa(b, centro, ancho, alto, grosor, color, hacia_arriba=False):
    x, y, z = centro
    medio = z + alto if hacia_arriba else z - alto
    b.tubo("Boca", [(x - ancho, y, z), (x, y - 0.015, medio), (x + ancho, y, z)], grosor, color)


# ---------------------------------------------------------------- los seis

def blip():
    """Bolita celeste con un ojo gigante, orejitas redondas y antena de resorte con bolita naranja. Hiperactivo."""
    b = Bichito("Blip")
    piel = mat("#6fbcff", 0.4)
    b.esfera("Cuerpo", (0, 0, 0.55), (0.46, 0.44, 0.45), piel)
    b.ojo((0, -0.33, 0.62), 0.24, "#2b6fd6", mirada=(0, -0.05), grande=True)
    for lado in (-1, 1):
        b.esfera("Oreja", (lado * 0.45, 0.02, 0.68), (0.06, 0.13, 0.13), piel)
        b.esfera("Oreja por dentro", (lado * 0.5, 0.0, 0.68), (0.025, 0.08, 0.08), mat("#a8dcff", 0.5))
    sonrisa(b, (0, -0.42, 0.34), 0.07, 0.035, 0.014, mat("#1d2340", 0.3))
    b.resorte("Resorte", (0, 0.02, 0.96), 0.3, 0.045, 4, 0.011, mat("#3d8fd8", 0.3))
    b.esfera("Bolita", (0, 0.02, 1.33), (0.075,) * 3, mat("#ff9f1c", 0.25))
    b.brazos(piel, 0.42, 0.4)
    b.patas(piel)
    return b


def pulpi():
    """Cúpula rosa con cuatro tentáculos, ojitos felices cerrados y moñito negro de concierto. Pianista."""
    b = Bichito("Pulpi")
    piel = mat("#ff86bd", 0.4)
    b.esfera("Cabeza", (0, 0, 0.7), (0.44, 0.42, 0.42), piel)
    b.esfera("Base", (0, 0.02, 0.33), (0.33, 0.31, 0.2), piel)
    # Dos tentáculos levantados (tocando un piano invisible) y dos apoyados atrás.
    for lado in (-1, 1):
        b.tubo("Tentáculo", [(lado * 0.2, -0.12, 0.34), (lado * 0.42, -0.22, 0.3), (lado * 0.6, -0.25, 0.45), (lado * 0.6, -0.27, 0.62), (lado * 0.5, -0.27, 0.66)],
               0.1, piel, radios=[1.0, 0.85, 0.65, 0.45, 0.3])
        b.tubo("Tentáculo", [(lado * 0.16, 0.12, 0.28), (lado * 0.36, 0.26, 0.12), (lado * 0.55, 0.24, 0.07), (lado * 0.62, 0.1, 0.12)],
               0.1, piel, radios=[1.0, 0.8, 0.55, 0.3])
        # Ojitos cerrados de felicidad (como "^ ^") y cachetes.
        x = lado * 0.15
        b.tubo("Ojo cerrado", [(x - 0.065, -0.4, 0.74), (x, -0.425, 0.79), (x + 0.065, -0.4, 0.74)], 0.016, mat("#2a1830", 0.3))
        b.esfera("Cachete", (lado * 0.26, -0.33, 0.62), (0.07, 0.03, 0.045), mat("#ff5c93", 0.6))
    sonrisa(b, (0, -0.415, 0.6), 0.05, 0.03, 0.013, mat("#2a1830", 0.3))
    # Moñito negro abajo de la cara.
    negro = mat("#16151c", 0.35, laca=0.6)
    for lado in (-1, 1):
        b.esfera("Moño", (lado * 0.075, -0.33, 0.38), (0.08, 0.035, 0.055), negro, giro=(0, lado * 15, 0))
    b.esfera("Nudo del moño", (0, -0.355, 0.38), (0.035,) * 3, negro)
    return b


def zorbo():
    """Cápsula violeta de tres ojos con párpados caídos, cuernitos, cresta y pulsera con tachas. Rockero."""
    b = Bichito("Zorbo")
    piel = mat("#9b6bff", 0.4)
    b.esfera("Cuerpo", (0, 0, 0.66), (0.36, 0.32, 0.6), piel)
    for x in (-0.17, 0, 0.17):
        b.ojo((x, -0.25, 0.88), 0.085, "#3a2a7a")
        # Párpado: tapa la mitad de arriba del ojo (cara de "re tranqui").
        b.esfera("Párpado", (x, -0.26, 0.915), (0.092, 0.06, 0.06), mat("#7e4fe6", 0.45))
    for lado in (-1, 1):
        b.cono("Cuernito", (lado * 0.2, 0.0, 1.2), 0.055, 0.17, mat("#ffe0a3", 0.4), giro=(0, lado * 22, 0))
    for i, y in enumerate((-0.16, -0.05, 0.06, 0.17)):
        b.cono("Cresta", (0, y, 1.25 - abs(y) * 0.6), 0.05, 0.2 - i * 0.015, mat("#ff4fa0", 0.4), giro=(-20, 0, 0))
    b.tubo("Boca", [(-0.08, -0.31, 0.7), (0.0, -0.325, 0.68), (0.09, -0.31, 0.72)], 0.012, mat("#2a1830", 0.3))
    # Un brazo en el volante y el otro arriba haciendo cuernitos, con la pulsera de tachas.
    b.esfera("Brazo", (-0.32, -0.12, 0.5), (0.06, 0.15, 0.06), piel, giro=(25, 0, 0))
    b.esfera("Mano", (-0.27, -0.27, 0.43), (0.075,) * 3, piel)
    b.esfera("Brazo arriba", (0.4, -0.05, 0.75), (0.06, 0.06, 0.17), piel, giro=(0, 20, 0))
    b.esfera("Mano arriba", (0.46, -0.06, 0.94), (0.075,) * 3, piel)
    for lado in (-1, 1):
        b.esfera("Dedo", (0.46 + lado * 0.035, -0.08, 1.04), (0.02, 0.02, 0.06), piel)
    negro = mat("#16151c", 0.3)
    bpy.ops.mesh.primitive_torus_add(major_radius=0.07, minor_radius=0.022, location=(0.44, -0.055, 0.86))
    pulsera = bpy.context.active_object
    bpy.ops.object.shade_smooth()
    b._colgar(pulsera, "Pulsera", negro)
    for i in range(6):
        a = i / 6 * 2 * math.pi
        b.cono("Tacha", (0.44 + math.cos(a) * 0.09, -0.055 + math.sin(a) * 0.09, 0.86), 0.018, 0.04, mat("#d8dde8", 0.2),
               giro=(0, 90, math.degrees(a)))
    b.patas(piel, 0.14)
    return b


def mukko():
    """Gota verde viscosa y algo traslúcida, con los ojos en dos antenitas. Pegote y cariñoso."""
    b = Bichito("Mukko")
    baba = mat("#6fdc7f", 0.12, vidrio=0.2, laca=1.0)
    b.gota("Cuerpo", (0, 0, 0.47), 0.45, 0.47, baba)
    for lado in (-1, 1):
        b.tubo("Antenita", [(lado * 0.06, 0.0, 0.82), (lado * 0.14, -0.02, 1.0), (lado * 0.24, -0.06, 1.15)], 0.035, baba, radios=[1.3, 0.9, 0.8])
        b.ojo((lado * 0.25, -0.1, 1.2), 0.085, "#2f7a3a", mirada=(-lado * 0.15, 0))
        # Gotas que chorrean, colgando de los costados del cuerpo.
        b.gota("Chorreado", (lado * 0.37, -0.18, 0.28), 0.08, 0.16, baba)
        b.esfera("Mano", (lado * 0.36, -0.3, 0.34), (0.08, 0.08, 0.07), baba)
    b.esfera("Boca", (0, -0.41, 0.42), (0.11, 0.04, 0.06), mat("#2d6b3a", 0.3))
    b.esfera("Lengua", (0, -0.425, 0.39), (0.055, 0.025, 0.025), mat("#ff8fb0", 0.4))
    return b


def kosmo():
    """Astronauta diminuto con un casco enorme: adentro solo se ven dos ojitos que brillan. Serio."""
    b = Bichito("Kosmo")
    traje = mat("#f2f4fa", 0.5)
    b.esfera("Traje", (0, 0, 0.33), (0.3, 0.27, 0.3), traje)
    b.esfera("Casco", (0, 0, 0.82), (0.4, 0.38, 0.38), mat("#f7f8fc", 0.25, laca=0.8))
    b.esfera("Visor", (0, -0.12, 0.82), (0.31, 0.28, 0.27), mat("#121833", 0.05, laca=1.0))
    for lado in (-1, 1):
        b.esfera("Ojito", (lado * 0.1, -0.385, 0.85), (0.05, 0.02, 0.07), mat("#3fe6ff", 0.2, 2.2), suave=0)
        b.cilindro("Oreja del casco", (lado * 0.39, 0, 0.82), 0.09, 0.08, mat("#4f8dff", 0.35), giro=(0, 90, 0))
    b.caja("Panel", (0, -0.26, 0.36), (0.2, 0.06, 0.13), 0.025, mat("#d8dce8", 0.4))
    for i, color in enumerate(("#ff5a5a", "#ffd23f", "#4f8dff")):
        b.esfera("Botón", (-0.06 + i * 0.06, -0.295, 0.37), (0.022, 0.012, 0.022), mat(color, 0.3, 1.0), suave=0)
    guante = mat("#c9cedb", 0.5)
    b.brazos(traje, 0.33, 0.3)
    for lado in (-1, 1):
        b.esfera("Guante", (lado * 0.24, -0.3, 0.26), (0.085,) * 3, guante)
    b.patas(traje, 0.13)
    return b


def klank():
    """Cajita de metal cobre oxidado con una pantallita de cara (ojos y sonrisa de píxeles celestes). Rígido."""
    b = Bichito("Klank")
    cobre = mat("#c8743d", 0.35, laca=0.3)
    oxido = mat("#7e9c7a", 0.8)
    b.caja("Cabeza", (0, 0, 0.88), (0.66, 0.5, 0.5), 0.09, cobre)
    b.caja("Pantalla", (0, -0.24, 0.88), (0.5, 0.04, 0.36), 0.04, mat("#0e1430", 0.15, laca=1.0))
    pixel = mat("#5ff2ff", 0.3, 5.0)
    for lado in (-1, 1):
        b.caja("Ojo", (lado * 0.1, -0.265, 0.93), (0.06, 0.015, 0.11), 0.01, pixel)
    for i, x in enumerate((-0.1, -0.05, 0.0, 0.05, 0.1)):
        b.caja("Sonrisa", (x, -0.265, 0.79 - (0.02 if abs(x) < 0.06 else 0.0) * (1 if i != 2 else 1.4)), (0.045, 0.015, 0.035), 0.008, pixel)
    for lado in (-1, 1):
        b.cilindro("Tornillo", (lado * 0.34, 0, 0.88), 0.07, 0.07, mat("#9aa2b8", 0.3), giro=(0, 90, 0))
    b.cilindro("Antenita", (0.18, 0.05, 1.18), 0.015, 0.14, mat("#9aa2b8", 0.3))
    b.esfera("Lucecita", (0.18, 0.05, 1.27), (0.035,) * 3, mat("#ff5a5a", 0.3, 3.0), suave=0)
    # Manchas de óxido verdoso.
    for lugar, escala in (((-0.25, -0.18, 1.08), (0.07, 0.02, 0.04)), ((0.27, -0.12, 0.7), (0.05, 0.02, 0.06)), ((0.3, 0.1, 1.1), (0.02, 0.06, 0.05))):
        b.esfera("Óxido", lugar, escala, oxido)
    b.cilindro("Cuello", (0, 0, 0.58), 0.08, 0.12, mat("#9aa2b8", 0.3))
    b.caja("Cuerpo", (0, 0, 0.32), (0.4, 0.32, 0.36), 0.07, mat("#b0622f", 0.4, laca=0.3))
    gris = mat("#9aa2b8", 0.3)
    for lado in (-1, 1):
        b.cilindro("Brazo", (lado * 0.26, -0.12, 0.34), 0.035, 0.26, gris, giro=(70, 0, 0))
        b.esfera("Mano", (lado * 0.26, -0.26, 0.29), (0.06,) * 3, gris)
        b.cilindro("Pie", (lado * 0.12, -0.03, 0.06), 0.08, 0.1, gris)
    return b


# ---------------------------------------------------------------- escena, render y exportación

bichitos = [f() for f in (blip, pulpi, zorbo, mukko, kosmo, klank)]
separacion = 1.3
for i, b in enumerate(bichitos):
    b.raiz.location = ((i - (len(bichitos) - 1) / 2) * separacion, 0, 0)
    b.raiz.rotation_euler = (0, 0, math.radians((i - 2.5) * -4))

bpy.ops.mesh.primitive_plane_add(size=60, location=(0, 0, 0))
piso = bpy.context.active_object
piso.data.materials.append(mat("#9584d6", 0.75))

mundo = bpy.data.worlds.new("Mundo")
mundo.use_nodes = True
mundo.node_tree.nodes["Background"].inputs[0].default_value = lineal("#8f7fd6")
mundo.node_tree.nodes["Background"].inputs[1].default_value = 0.7
escena.world = mundo


def luz(lugar, energia, tamano, color, mira=(0, 0, 0.6)):
    bpy.ops.object.light_add(type="AREA", location=lugar)
    l = bpy.context.active_object
    l.data.energy, l.data.size, l.data.color = energia, tamano, color
    l.rotation_mode = "QUATERNION"
    l.rotation_quaternion = (Vector(mira) - Vector(lugar)).to_track_quat("-Z", "Y")


luz((-4, -7, 6), 1000, 6, (1.0, 0.95, 0.88))
luz((6, -4, 3), 450, 6, (0.75, 0.82, 1.0))
luz((0, 5, 4), 700, 6, (1.0, 0.85, 0.95))

bpy.ops.object.camera_add(location=(0, -12.5, 2.0))
camara = bpy.context.active_object
camara.rotation_mode = "QUATERNION"
camara.rotation_quaternion = (Vector((0, 0, 0.62)) - camara.location).to_track_quat("-Z", "Y")
camara.data.lens = 46
escena.camera = camara

motores = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
escena.render.engine = next((m for m in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT") if m in motores), "CYCLES")
try:
    escena.eevee.use_raytracing = True
except Exception:
    pass
escena.view_settings.view_transform = "Standard"
escena.render.resolution_x, escena.render.resolution_y = 1800, 760
escena.render.filepath = archivo_render
bpy.ops.render.render(write_still=True)

# Cada bichito a su .glb, en el origen y con las curvas pasadas a malla.
for b in bichitos:
    b.raiz.location = (0, 0, 0)
    b.raiz.rotation_euler = (0, 0, 0)
for obj in [o for o in bpy.data.objects if o.type == "CURVE"]:
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.convert(target="MESH")
for b in bichitos:
    bpy.ops.object.select_all(action="DESELECT")
    b.raiz.select_set(True)
    for hijo in b.raiz.children_recursive:
        hijo.select_set(True)
    # FBX, que Unity abre sin paquetes extra, con Y para arriba. En Unity el bichito queda mirando hacia
    # −Z: ModeloKart lo da vuelta al sentarlo en el kart.
    bpy.ops.export_scene.fbx(filepath=os.path.join(salida, f"{b.nombre}.fbx"), use_selection=True,
                             object_types={"EMPTY", "MESH"}, use_mesh_modifiers=True,
                             apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
                             bake_space_transform=True, add_leaf_bones=False, mesh_smooth_type="FACE")
print("LISTO", escena.render.engine, salida)
