using NUnit.Framework;

namespace SoulBlocks.Tests
{
    public class CargoTests
    {
        [Test]
        public void AddAcceptsWhenRoomAvailable()
        {
            int cargo = 10;
            int accepted = Cargo.Add(ref cargo, 5, 100);
            Assert.AreEqual(5, accepted);
            Assert.AreEqual(15, cargo);
        }

        [Test]
        public void AddClampsToRemainingCapacity()
        {
            int cargo = 95;
            int accepted = Cargo.Add(ref cargo, 20, 100);
            Assert.AreEqual(5, accepted);
            Assert.AreEqual(100, cargo);
        }

        [Test]
        public void AddAtFullCapacityAcceptsNothing()
        {
            int cargo = 100;
            int accepted = Cargo.Add(ref cargo, 1, 100);
            Assert.AreEqual(0, accepted);
            Assert.AreEqual(100, cargo);
        }

        [Test]
        public void DepositMovesAllCargoToReserveAndEmptiesCargo()
        {
            int cargo = 42, reserve = 8;
            int amount = Cargo.Deposit(ref cargo, ref reserve);
            Assert.AreEqual(42, amount);
            Assert.AreEqual(0, cargo);
            Assert.AreEqual(50, reserve);
        }

        [Test]
        public void DepositOfEmptyCargoIsANoOp()
        {
            int cargo = 0, reserve = 8;
            int amount = Cargo.Deposit(ref cargo, ref reserve);
            Assert.AreEqual(0, amount);
            Assert.AreEqual(8, reserve);
        }
    }
}
