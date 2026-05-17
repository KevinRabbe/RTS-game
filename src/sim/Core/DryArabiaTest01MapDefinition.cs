using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Core
{
    public static class DryArabiaTest01MapDefinition
    {
        public const string MapName = "DryArabiaTest01";

        public static readonly FixedVector2 Player0TownCenterZone = FixedVector2.FromInts(24, 48);
        public static readonly FixedVector2 Player1TownCenterZone = FixedVector2.FromInts(104, 48);

        public static FixedVector2 GetTownCenterZone(int playerIndex)
        {
            return playerIndex == 0 ? Player0TownCenterZone : Player1TownCenterZone;
        }

        public static FixedVector2 GetSpawnPosition(int playerIndex)
        {
            FixedVector2 tc = GetTownCenterZone(playerIndex);
            return new FixedVector2(tc.X, tc.Y + Fixed.FromInt(2));
        }

        public static FixedVector2[] GetNearbyResourcePositions(int playerIndex, ResourceType resourceType)
        {
            if (playerIndex == 0)
            {
                switch (resourceType)
                {
                    case ResourceType.Food:
                        return new[] { FixedVector2.FromInts(30, 48), FixedVector2.FromInts(31, 52) };
                    case ResourceType.Wood:
                        return new[] { FixedVector2.FromInts(22, 57), FixedVector2.FromInts(19, 58) };
                    case ResourceType.Gold:
                        return new[] { FixedVector2.FromInts(18, 47), FixedVector2.FromInts(16, 44) };
                }
            }

            switch (resourceType)
            {
                case ResourceType.Food:
                    return new[] { FixedVector2.FromInts(98, 48), FixedVector2.FromInts(97, 52) };
                case ResourceType.Wood:
                    return new[] { FixedVector2.FromInts(106, 57), FixedVector2.FromInts(109, 58) };
                case ResourceType.Gold:
                    return new[] { FixedVector2.FromInts(110, 47), FixedVector2.FromInts(112, 44) };
                default:
                    return new FixedVector2[0];
            }
        }
    }
}
