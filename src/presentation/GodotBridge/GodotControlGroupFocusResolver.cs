namespace RtsGame.Presentation.GodotBridge
{
    public static class GodotControlGroupFocusResolver
    {
        public static bool TryResolveCenterRaw(GodotFrameDto frame, int[] unitIds, out long centerXRaw, out long centerYRaw)
        {
            long sumX = 0;
            long sumY = 0;
            int count = 0;

            for (int i = 0; i < unitIds.Length; i++)
            {
                GodotUnitStatusDto? status = FindUnitStatus(frame, unitIds[i]);
                if (status == null)
                {
                    continue;
                }

                sumX += status.PositionXRaw;
                sumY += status.PositionYRaw;
                count++;
            }

            if (count == 0)
            {
                centerXRaw = 0;
                centerYRaw = 0;
                return false;
            }

            centerXRaw = sumX / count;
            centerYRaw = sumY / count;
            return true;
        }

        private static GodotUnitStatusDto? FindUnitStatus(GodotFrameDto frame, int unitId)
        {
            for (int i = 0; i < frame.UnitStatuses.Length; i++)
            {
                if (frame.UnitStatuses[i].UnitId == unitId)
                {
                    return frame.UnitStatuses[i];
                }
            }

            return null;
        }
    }
}
