using RtsGame.Presentation.Visuals;

namespace RtsGame.Presentation.GodotBridge
{
    public static class GodotTradeRouteRouter
    {
        private const int TradePostBuildingTypeId = 3;
        private const int TradeCartUnitTypeId = 5;

        public static int FindLocalTradePostAt(GodotFrameDto frame, int localPlayerIndex, long xRaw, long yRaw)
        {
            for (int i = 0; i < frame.Primitives.Length; i++)
            {
                GodotPrimitiveDto primitive = frame.Primitives[i];
                if (primitive.Kind != (int)VisualPrimitiveKind.BuildingRectangle
                    || primitive.TypeId != TradePostBuildingTypeId
                    || primitive.OwnerPlayerIndex != localPlayerIndex)
                {
                    continue;
                }

                if (GodotPrimitiveHitTest.ContainsPoint(primitive, xRaw, yRaw))
                {
                    return primitive.EntityId;
                }
            }

            return 0;
        }

        public static int FindSelectedTradeCart(GodotFrameDto frame, int[] selectedUnitIds)
        {
            if (selectedUnitIds.Length == 0)
            {
                return 0;
            }

            int selectedUnitId = selectedUnitIds[0];
            for (int i = 0; i < frame.UnitStatuses.Length; i++)
            {
                if (frame.UnitStatuses[i].UnitId == selectedUnitId && frame.UnitStatuses[i].UnitTypeId == TradeCartUnitTypeId)
                {
                    return selectedUnitId;
                }
            }

            return 0;
        }
    }
}
