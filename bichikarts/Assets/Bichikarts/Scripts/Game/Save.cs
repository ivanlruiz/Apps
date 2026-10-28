using Bichikarts.Core;
using UnityEngine;

namespace Bichikarts
{
    /// <summary>Lo que se guarda en el dispositivo (PlayerPrefs): estrellas, bichitos elegidos y opciones.</summary>
    public static class Save
    {
        private const string Prefix = "bichikarts.";

        public static int Stars
        {
            get => PlayerPrefs.GetInt(Prefix + "estrellas", 0);
            set { PlayerPrefs.SetInt(Prefix + "estrellas", Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        public static Look LoadLook(int player)
        {
            string json = PlayerPrefs.GetString(Prefix + "look" + player, "");
            if (string.IsNullOrEmpty(json)) return Catalog.DefaultLook(player);
            try
            {
                var look = JsonUtility.FromJson<Look>(json);
                return look ?? Catalog.DefaultLook(player);
            }
            catch (System.Exception)
            {
                return Catalog.DefaultLook(player);
            }
        }

        public static void StoreLook(int player, Look look)
        {
            PlayerPrefs.SetString(Prefix + "look" + player, JsonUtility.ToJson(look));
            PlayerPrefs.Save();
        }

        public static string GetString(string key, string fallback) => PlayerPrefs.GetString(Prefix + key, fallback);

        public static void SetString(string key, string value)
        {
            PlayerPrefs.SetString(Prefix + key, value);
            PlayerPrefs.Save();
        }

        public static int GetInt(string key, int fallback) => PlayerPrefs.GetInt(Prefix + key, fallback);

        public static void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(Prefix + key, value);
            PlayerPrefs.Save();
        }

        /// <summary>Si un aspecto guardado usa algo que no está desbloqueado (o que ya no existe), lo corrige.</summary>
        public static Look Sanitize(Look look, int player, int stars)
        {
            var d = Catalog.DefaultLook(player);
            var clean = look.Clone();
            if (!Catalog.IsUnlocked(Catalog.Find(Catalog.Species, clean.species), stars) || Catalog.Find(Catalog.Species, clean.species).Id != clean.species) clean.species = d.species;
            if (!Catalog.IsUnlocked(Catalog.Find(Catalog.KartStyles, clean.style), stars) || Catalog.Find(Catalog.KartStyles, clean.style).Id != clean.style) clean.style = d.style;
            if (!Catalog.IsUnlocked(Catalog.Find(Catalog.Hats, clean.hat), stars) || Catalog.Find(Catalog.Hats, clean.hat).Id != clean.hat) clean.hat = d.hat;
            if (!ColorUtility.TryParseHtmlString(clean.body, out _)) clean.body = d.body;
            if (!ColorUtility.TryParseHtmlString(clean.kart, out _)) clean.kart = d.kart;
            return clean;
        }
    }
}
