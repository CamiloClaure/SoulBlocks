using UnityEngine;

namespace SoulBlocks
{
    // Pure cargo math, no RunState coupling: how many of an incoming amount fit under capacity,
    // and moving the whole load into the reserve.
    public static class Cargo
    {
        // Returns how many of 'amount' actually fit; cargoSouls grows by that amount.
        public static int Add(ref int cargoSouls, int amount, int capacity)
        {
            if (amount <= 0) return 0;
            int room = Mathf.Max(0, capacity - cargoSouls);
            int accepted = Mathf.Min(room, amount);
            cargoSouls += accepted;
            return accepted;
        }

        // Moves the whole current cargo into the reserve; returns the amount moved.
        public static int Deposit(ref int cargoSouls, ref int reserveSouls)
        {
            int amount = cargoSouls;
            reserveSouls += amount;
            cargoSouls = 0;
            return amount;
        }
    }
}
