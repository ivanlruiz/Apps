using System.Collections.Generic;

namespace Bichikarts.Core
{
    /// <summary>
    /// Las salas. Coordenadas en píxeles de sala (1600 x 900, Y hacia abajo). El marco mide 50,
    /// así que el piso está en y = 850, el techo en y = 50 y las paredes en x = 50 y x = 1550.
    /// </summary>
    public static class Levels
    {
        private const float FloorY = Tuning.RoomHeight - Tuning.WallThickness;
        private const float CeilingY = Tuning.WallThickness;
        private const float LeftX = Tuning.WallThickness;
        private const float RightX = Tuning.RoomWidth - Tuning.WallThickness;
        private const float SpawnY = FloorY - Tuning.Half;

        private static PipeEnd OnFloor(float x) => new PipeEnd(x, FloorY, Dir.Up);
        private static PipeEnd OnCeiling(float x) => new PipeEnd(x, CeilingY, Dir.Down);
        private static PipeEnd OnLeftWall(float y) => new PipeEnd(LeftX, y, Dir.Right);
        private static PipeEnd OnRightWall(float y) => new PipeEnd(RightX, y, Dir.Left);
        private static PipeEnd OnTop(float x, float y) => new PipeEnd(x, y, Dir.Up);

        private static List<Level> _all;

        public static IReadOnlyList<Level> All => _all ??= Build();

        public static Level Get(string id)
        {
            foreach (var l in All)
                if (l.Id == id) return l;
            return All[0];
        }

        private static List<Level> Build()
        {
            return new List<Level> { Cocina(), Sotano(), Estacion(), Living() };
        }

        private static Level Cocina()
        {
            var l = new Level
            {
                Id = "cocina",
                Name = "La Cocina",
                Description = "Una cocina flotando en órbita. La miguita está sobre la mesa.",
                Theme = "cocina",
                DefaultFood = "miguita",
            };
            l.AddFrame()
                .Block("mesa", 560, 700, 480, 28)
                .Block("estante", 50, 430, 250, 26)
                .Block("estante", 1300, 430, 250, 26)
                .Block("colgante", 640, 170, 320, 26)
                .AddPipe("#e5484d", OnFloor(400), OnCeiling(1200), 400, FloorY, 400, 560, 1200, 560, 1200, CeilingY)
                .AddPipe("#3e8ef7", OnFloor(1150), OnCeiling(450), 1150, FloorY, 1150, 610, 450, 610, 450, CeilingY)
                .Spawns(140, SpawnY, 1460, SpawnY)
                .FoodAt(800, 668)
                .Add(new Decor("ventana", 600, 250, 400, 260))
                .Add(new Decor("pata", 585, 728, 20, 122))
                .Add(new Decor("pata", 995, 728, 20, 122))
                .Add(new Decor("reloj", 1420, 230))
                .Add(new Decor("cadena", 660, 50, 4, 120))
                .Add(new Decor("cadena", 936, 50, 4, 120));
            return l;
        }

        private static Level Sotano()
        {
            var l = new Level
            {
                Id = "sotano",
                Name = "El Sótano de Caños",
                Description = "Cuatro tuberías y una caldera. El queso espera arriba de la caldera.",
                Theme = "sotano",
                DefaultFood = "queso",
            };
            l.AddFrame()
                .Block("caldera", 700, 720, 200, 130)
                .Block("reja", 230, 500, 260, 26)
                .Block("reja", 1110, 500, 260, 26)
                .AddPipe("#d9822b", OnFloor(170), OnCeiling(1430), 170, FloorY, 170, 300, 1430, 300, 1430, CeilingY)
                .AddPipe("#30a46c", OnFloor(1380), OnCeiling(220), 1380, FloorY, 1380, 350, 220, 350, 220, CeilingY)
                .AddPipe("#3e8ef7", OnLeftWall(650), OnRightWall(650), LeftX, 650, 120, 650, 120, 790, 1480, 790, 1480, 650, RightX, 650)
                .AddPipe("#e93d82", OnTop(290, 500), OnTop(1310, 500), 290, 500, 290, 580, 1310, 580, 1310, 500)
                .Spawns(110, SpawnY, 1490, SpawnY)
                .FoodAt(800, 690)
                .Add(new Decor("lamparita", 420, 50))
                .Add(new Decor("lamparita", 1180, 50))
                .Add(new Decor("valvula", 560, 420))
                .Add(new Decor("valvula", 1040, 420));
            return l;
        }

        private static Level Estacion()
        {
            var l = new Level
            {
                Id = "estacion",
                Name = "Estación Galletita",
                Description = "Una estación espacial con plataformas flotantes y la galletita en el medio.",
                Theme = "estacion",
                DefaultFood = "galletita",
            };
            l.AddFrame()
                .Block("plataforma", 620, 700, 360, 26)
                .Block("plataforma", 620, 300, 360, 26)
                .Block("pilar", 330, 380, 26, 220)
                .Block("pilar", 1244, 380, 26, 220)
                .Block("repisa", 50, 620, 170, 26)
                .Block("repisa", 1380, 620, 170, 26)
                .Block("repisa", 50, 250, 170, 26)
                .Block("repisa", 1380, 250, 170, 26)
                .AddPipe("#f5c518", OnFloor(420), OnCeiling(1180), 420, FloorY, 420, 770, 1180, 770, 1180, CeilingY)
                .AddPipe("#2ec4d6", OnFloor(1130), OnCeiling(470), 1130, FloorY, 1130, 810, 470, 810, 470, CeilingY)
                .AddPipe("#9b6cf0", OnLeftWall(450), OnRightWall(450), LeftX, 450, 110, 450, 110, 160, 1490, 160, 1490, 450, RightX, 450)
                .Spawns(130, SpawnY, 1470, SpawnY)
                .FoodAt(800, 540)
                .Add(new Decor("ojo_buey", 450, 420, 150, 150))
                .Add(new Decor("ojo_buey", 1000, 420, 150, 150))
                .Add(new Decor("planeta", 800, 150, 120, 120))
                .Add(new Decor("luces", 0, 0));
            return l;
        }

        private static Level Living()
        {
            var l = new Level
            {
                Id = "living",
                Name = "El Living",
                Description = "Sofá, biblioteca y un ventilador de techo. El alfajor está en la mesita.",
                Theme = "living",
                DefaultFood = "alfajor",
            };
            l.AddFrame()
                .Block("sofa", 200, 740, 360, 110)
                .Block("mesita", 700, 760, 200, 26)
                .Block("biblioteca", 1290, 330, 260, 24)
                .Block("biblioteca", 1290, 560, 260, 24)
                .Block("ventilador", 680, 140, 240, 20)
                .AddPipe("#ff8c42", OnFloor(620), OnRightWall(450), 620, FloorY, 620, 450, RightX, 450)
                .AddPipe("#30a46c", OnFloor(980), OnLeftWall(300), 980, FloorY, 980, 300, LeftX, 300)
                .AddPipe("#e93d82", OnCeiling(300), OnCeiling(1300), 300, CeilingY, 300, 110, 1300, 110, 1300, CeilingY)
                .Spawns(110, SpawnY, 1490, SpawnY)
                .FoodAt(800, 728)
                .Add(new Decor("ventana", 140, 200, 300, 220))
                .Add(new Decor("cuadro", 980, 380, 160, 120))
                .Add(new Decor("pata", 715, 786, 14, 64))
                .Add(new Decor("pata", 871, 786, 14, 64))
                .Add(new Decor("varilla", 798, 50, 4, 90))
                .Add(new Decor("lampara", 1150, 850))
                .Add(new Decor("libros", 1290, 330))
                .Add(new Decor("libros", 1290, 560));
            return l;
        }
    }
}
