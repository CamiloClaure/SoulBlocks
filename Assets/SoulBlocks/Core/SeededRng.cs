using System.Collections.Generic;

namespace SoulBlocks
{
    // Own deterministic PRNG: SplitMix64. Never System.Random or UnityEngine.Random, so
    // replaying a seed gives the exact same sequence on every platform and Unity version.
    public sealed class SeededRng
    {
        ulong state;

        public SeededRng(ulong seed) { state = seed; }
        public SeededRng(int seed) : this(unchecked((ulong)seed)) { }

        // Derives an independent stream from a shared seed: mixing in the FNV-1a hash of
        // "name" means a new system (new Stream name) never shifts any other system's sequence.
        public static SeededRng Stream(int seed, string name) => new SeededRng(unchecked((ulong)seed) ^ Fnv1a(name));

        static ulong Fnv1a(string text)
        {
            unchecked
            {
                ulong hash = 14695981039346656037UL;
                for (int i = 0; i < text.Length; i++)
                {
                    hash ^= text[i];
                    hash *= 1099511628211UL;
                }
                return hash;
            }
        }

        ulong NextRaw()
        {
            unchecked
            {
                state += 0x9E3779B97F4A7C15UL;
                ulong z = state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        // [0, 1): top 24 bits, enough precision for a float mantissa.
        public float NextFloat() => (uint)(NextRaw() >> 40) / (float)(1 << 24);

        public int Next(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            // ponytail: modulo has a tiny bias for huge ranges, irrelevant at map-gen scale.
            uint range = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextRaw() % range);
        }

        public float Range(float min, float max) => min + NextFloat() * (max - min);

        public bool Chance(float p) => NextFloat() < p;

        public T Pick<T>(IList<T> list) => list[Next(0, list.Count)];

        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Next(0, i + 1);
                T swap = list[i];
                list[i] = list[j];
                list[j] = swap;
            }
        }
    }
}
