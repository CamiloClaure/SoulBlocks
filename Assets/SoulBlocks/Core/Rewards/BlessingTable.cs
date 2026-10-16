using System.Collections.Generic;

namespace SoulBlocks
{
    // Fixed pool of blessings (plan 1.6/1.7/1.8). Offer picks up to `count` distinct ids,
    // shuffled deterministically per (seed, prison id) so the same branch-end prison always
    // offers the same set for a given seed. `excluded` keeps the offer to options actually
    // useful with the player's current abilities (plan: "solo opciones utiles").
    public static class BlessingTable
    {
        public static readonly string[] AllIds = { "dash_extra", "pico_cadena", "energia", "carga" };

        public static List<string> Offer(int seed, int prisonId, int count, ICollection<string> excluded)
        {
            var pool = new List<string>();
            foreach (var id in AllIds)
                if (excluded == null || !excluded.Contains(id)) pool.Add(id);

            SeededRng.Stream(seed, "blessing.offer." + prisonId).Shuffle(pool);

            int take = count < pool.Count ? count : pool.Count;
            if (take < 0) take = 0;
            return pool.GetRange(0, take);
        }
    }
}
