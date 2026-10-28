using System;
using System.Collections.Generic;

namespace Bichikarts.Core
{
    /// <summary>Algo que se elige en el garaje. Cost es la cantidad de estrellas que hay que juntar para usarlo.</summary>
    public sealed class Item
    {
        public readonly string Id;
        public readonly string Name;
        public readonly int Cost;
        public readonly string Color;

        public Item(string id, string name, int cost, string color = "")
        {
            Id = id; Name = name; Cost = cost; Color = color;
        }
    }

    /// <summary>Cómo se ve un jugador: bichito, colores, karting y accesorio.</summary>
    [Serializable]
    public sealed class Look
    {
        public string species = "blip";
        public string body = "#79f0c3";
        public string kart = "#2ec4f1";
        public string style = "clasico";
        public string hat = "casco";

        public Look Clone() => (Look)MemberwiseClone();
    }

    public static class Catalog
    {
        public static readonly Item[] Species =
        {
            new Item("blip", "Blip", 0, "#79f0c3"),
            new Item("zorbo", "Zorbo", 0, "#b48cff"),
            new Item("pulpi", "Pulpi", 0, "#ff8fb1"),
            new Item("moco", "Moco", 3, "#b5e655"),
            new Item("chispa", "Chispa", 6, "#ffd23f"),
            new Item("glub", "Glub", 10, "#5fb8ff"),
            new Item("cometa", "Cometa", 15, "#ff7b47"),
            new Item("noqui", "Ñoqui", 21, "#f2d49b"),
        };

        public static readonly string[] BodyColors =
        {
            "#79f0c3", "#b48cff", "#ff8fb1", "#b5e655", "#ffd23f",
            "#5fb8ff", "#ff7b47", "#f2d49b", "#f4f1ff", "#8a8fa8",
        };

        public static readonly string[] KartColors =
        {
            "#e8443a", "#ff9f1c", "#ffd23f", "#3ddc84", "#2ec4f1",
            "#3a6df0", "#9b5de5", "#f15bb5", "#f4f1de", "#4a4f63",
        };

        public static readonly Item[] KartStyles =
        {
            new Item("clasico", "Clásico", 0),
            new Item("cohete", "Cohete", 5),
            new Item("tostadora", "Tostadora", 9),
        };

        public static readonly Item[] Hats =
        {
            new Item("nada", "Nada", 0),
            new Item("casco", "Casco espacial", 0),
            new Item("fiesta", "Gorro de fiesta", 0),
            new Item("mono", "Moño", 2),
            new Item("anteojos", "Anteojos de sol", 4),
            new Item("chef", "Gorro de chef", 8),
            new Item("corona", "Corona", 12),
            new Item("colador", "Colador", 18),
        };

        public static readonly Item[] Foods =
        {
            new Item("miguita", "Miguita de pan", 0),
            new Item("queso", "Queso", 0),
            new Item("galletita", "Galletita", 0),
            new Item("alfajor", "Alfajor", 0),
        };

        /// <summary>Colores de cada jugador en el HUD y las marcas (no dependen del karting elegido).</summary>
        public static readonly string[] PlayerColors = { "#4cc9ff", "#ff7b5c" };

        public static Item Find(Item[] list, string id)
        {
            foreach (var it in list)
                if (it.Id == id) return it;
            return list[0];
        }

        public static bool IsUnlocked(Item item, int stars) => stars >= item.Cost;

        public static Look DefaultLook(int player)
        {
            return player == 0
                ? new Look { species = "blip", body = "#79f0c3", kart = "#2ec4f1", style = "clasico", hat = "casco" }
                : new Look { species = "zorbo", body = "#b48cff", kart = "#ff9f1c", style = "clasico", hat = "fiesta" };
        }

        /// <summary>Un aspecto al azar usando solo lo desbloqueado.</summary>
        public static Look RandomLook(Random rng, int stars)
        {
            var species = PickUnlocked(Species, rng, stars);
            return new Look
            {
                species = species.Id,
                body = rng.Next(3) == 0 ? BodyColors[rng.Next(BodyColors.Length)] : species.Color,
                kart = KartColors[rng.Next(KartColors.Length)],
                style = PickUnlocked(KartStyles, rng, stars).Id,
                hat = PickUnlocked(Hats, rng, stars).Id,
            };
        }

        private static Item PickUnlocked(Item[] list, Random rng, int stars)
        {
            var open = new List<Item>();
            foreach (var it in list)
                if (IsUnlocked(it, stars)) open.Add(it);
            return open[rng.Next(open.Count)];
        }

        /// <summary>Estrellas que da una partida terminada.</summary>
        public static int StarsFor(bool won, bool vsCpu, CpuDifficulty difficulty)
        {
            if (!won) return 1;
            if (!vsCpu) return 3;
            return difficulty switch
            {
                CpuDifficulty.Easy => 2,
                CpuDifficulty.Hard => 5,
                _ => 3,
            };
        }

        /// <summary>Lo que se desbloquea al pasar de "before" a "after" estrellas.</summary>
        public static List<Item> NewlyUnlocked(int before, int after)
        {
            var result = new List<Item>();
            foreach (var list in new[] { Species, KartStyles, Hats })
                foreach (var it in list)
                    if (it.Cost > before && it.Cost <= after) result.Add(it);
            return result;
        }
    }
}
