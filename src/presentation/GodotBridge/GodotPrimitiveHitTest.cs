namespace RtsGame.Presentation.GodotBridge
{
    public static class GodotPrimitiveHitTest
    {
        public static bool ContainsPoint(GodotPrimitiveDto primitive, long xRaw, long yRaw)
        {
            long halfSize = primitive.SizeRaw / 2;
            return xRaw >= primitive.XRaw - halfSize
                && xRaw <= primitive.XRaw + halfSize
                && yRaw >= primitive.YRaw - halfSize
                && yRaw <= primitive.YRaw + halfSize;
        }
    }
}
