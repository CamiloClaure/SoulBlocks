using NUnit.Framework;
using SoulBlocks;

namespace SoulBlocks.Tests
{
    public class StatSheetTests
    {
        [Test]
        public void GetReturnsBalanceBaseWithNoModifiers()
        {
            var sheet = new StatSheet();
            Assert.AreEqual(Balance.MoveSpeed, sheet.Get(StatId.MoveSpeed), .0001f);
        }

        [Test]
        public void AddAndMulCombineAcrossSources()
        {
            var sheet = new StatSheet();
            sheet.SetModifier("upgrade", StatId.CargoCapacity, 50f);
            sheet.SetModifier("blessing", StatId.CargoCapacity, 0f, 2f);
            // (base + add) * mul = (100 + 50) * 2 = 300
            Assert.AreEqual((Balance.CargoCapacity + 50f) * 2f, sheet.Get(StatId.CargoCapacity), .0001f);
        }

        [Test]
        public void SetModifierReplacesSameSource()
        {
            var sheet = new StatSheet();
            sheet.SetModifier("upgrade", StatId.PickupRadius, 1f);
            sheet.SetModifier("upgrade", StatId.PickupRadius, 3f);
            Assert.AreEqual(Balance.PickupRadius + 3f, sheet.Get(StatId.PickupRadius), .0001f);
        }

        [Test]
        public void ClearSourceRemovesItsModifiers()
        {
            var sheet = new StatSheet();
            sheet.SetModifier("upgrade", StatId.EnergyMax, 10f);
            sheet.ClearSource("upgrade");
            Assert.AreEqual(Balance.EnergyMax, sheet.Get(StatId.EnergyMax), .0001f);
        }

        [Test]
        public void ChangedFiresOnSetAndClear()
        {
            var sheet = new StatSheet();
            int fired = 0;
            sheet.Changed += () => fired++;
            sheet.SetModifier("upgrade", StatId.DashCharges, 1f);
            sheet.ClearSource("upgrade");
            Assert.AreEqual(2, fired);
        }

        [Test]
        public void GetIntRounds()
        {
            var sheet = new StatSheet();
            sheet.SetModifier("test", StatId.RayCharges, .6f);
            Assert.AreEqual(System.Convert.ToInt32(System.Math.Round(Balance.RayCharges + .6f)), sheet.GetInt(StatId.RayCharges));
        }
    }
}
