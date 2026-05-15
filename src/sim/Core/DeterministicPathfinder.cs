using RtsGame.Sim.Data;

namespace RtsGame.Sim.Core
{
    public static class DeterministicPathfinder
    {
        private const int CardinalCost = 10;

        private static readonly int[] NeighborOffsetX = new[] { 1, 0, -1, 0 };
        private static readonly int[] NeighborOffsetY = new[] { 0, 1, 0, -1 };

        public static bool TryFindNextTile(GameState state, int startX, int startY, int targetX, int targetY, out int nextX, out int nextY)
        {
            nextX = startX;
            nextY = startY;
            int width = state.MapState.WidthTiles;
            int height = state.MapState.HeightTiles;
            if (!IsInBounds(startX, startY, width, height) || !IsInBounds(targetX, targetY, width, height))
            {
                return false;
            }

            if (startX == targetX && startY == targetY)
            {
                return true;
            }

            if (SpatialRules.IsTileBlockedForUnitMovement(state, targetX, targetY))
            {
                return false;
            }

            int tileCount = width * height;
            var open = new bool[tileCount];
            var closed = new bool[tileCount];
            var gCost = new int[tileCount];
            var hCost = new int[tileCount];
            var parent = new int[tileCount];
            for (int i = 0; i < tileCount; i++)
            {
                gCost[i] = int.MaxValue;
                parent[i] = -1;
            }

            int startIndex = ToIndex(startX, startY, width);
            int targetIndex = ToIndex(targetX, targetY, width);
            gCost[startIndex] = 0;
            hCost[startIndex] = Heuristic(startX, startY, targetX, targetY);
            open[startIndex] = true;

            while (TrySelectOpen(open, closed, gCost, hCost, out int currentIndex))
            {
                if (currentIndex == targetIndex)
                {
                    return TryResolveNextStep(parent, currentIndex, startIndex, width, out nextX, out nextY);
                }

                open[currentIndex] = false;
                closed[currentIndex] = true;
                int currentX = currentIndex % width;
                int currentY = currentIndex / width;
                for (int neighbor = 0; neighbor < NeighborOffsetX.Length; neighbor++)
                {
                    int neighborX = currentX + NeighborOffsetX[neighbor];
                    int neighborY = currentY + NeighborOffsetY[neighbor];
                    if (!IsInBounds(neighborX, neighborY, width, height) || SpatialRules.IsTileBlockedForUnitMovement(state, neighborX, neighborY))
                    {
                        continue;
                    }

                    int neighborIndex = ToIndex(neighborX, neighborY, width);
                    if (closed[neighborIndex])
                    {
                        continue;
                    }

                    int tentativeG = gCost[currentIndex] + CardinalCost;
                    if (!open[neighborIndex] || tentativeG < gCost[neighborIndex])
                    {
                        parent[neighborIndex] = currentIndex;
                        gCost[neighborIndex] = tentativeG;
                        hCost[neighborIndex] = Heuristic(neighborX, neighborY, targetX, targetY);
                        open[neighborIndex] = true;
                    }
                }
            }

            return false;
        }

        private static bool TryResolveNextStep(int[] parent, int targetIndex, int startIndex, int width, out int nextX, out int nextY)
        {
            int current = targetIndex;
            int previous = targetIndex;
            while (current != startIndex)
            {
                previous = current;
                current = parent[current];
                if (current < 0)
                {
                    nextX = 0;
                    nextY = 0;
                    return false;
                }
            }

            nextX = previous % width;
            nextY = previous / width;
            return true;
        }

        private static bool TrySelectOpen(bool[] open, bool[] closed, int[] gCost, int[] hCost, out int selectedIndex)
        {
            selectedIndex = -1;
            int selectedF = int.MaxValue;
            int selectedH = int.MaxValue;
            for (int i = 0; i < open.Length; i++)
            {
                if (!open[i] || closed[i])
                {
                    continue;
                }

                int f = gCost[i] + hCost[i];
                if (selectedIndex < 0 || f < selectedF || (f == selectedF && (hCost[i] < selectedH || (hCost[i] == selectedH && i < selectedIndex))))
                {
                    selectedIndex = i;
                    selectedF = f;
                    selectedH = hCost[i];
                }
            }

            return selectedIndex >= 0;
        }

        private static int Heuristic(int fromX, int fromY, int toX, int toY)
        {
            return (Abs(fromX - toX) + Abs(fromY - toY)) * CardinalCost;
        }

        private static int Abs(int value)
        {
            return value < 0 ? -value : value;
        }

        private static bool IsInBounds(int x, int y, int width, int height)
        {
            return x >= 0 && y >= 0 && x < width && y < height;
        }

        private static int ToIndex(int x, int y, int width)
        {
            return y * width + x;
        }
    }
}
