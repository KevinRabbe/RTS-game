using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RtsGame.GCodeExport
{
    public static class GCodeExporter
    {
        public const string DemoWarning = "Demo only - not for real CNC machines";

        public static string ExportPath(IEnumerable<GCodePathPoint> points, GCodeExportConfig config)
        {
            var builder = new StringBuilder();
            builder.AppendLine("(Demo only - not for real CNC machines)");
            builder.AppendLine("(RTS unit movement path export)");
            builder.AppendLine("G21");
            builder.AppendLine("G90");
            builder.Append("G0 Z").AppendLine(Format(config.SafeZ));

            bool hasPoint = false;
            foreach (GCodePathPoint point in points)
            {
                decimal x = point.XTile * config.ScaleMmPerTile;
                decimal y = point.YTile * config.ScaleMmPerTile;
                builder.Append("(tick ").Append(point.Tick.ToString(CultureInfo.InvariantCulture)).AppendLine(")");
                builder.Append("G0 X").Append(Format(x)).Append(" Y").AppendLine(Format(y));
                if (!hasPoint)
                {
                    builder.Append("G1 Z").Append(Format(config.DrawZ)).Append(" F").AppendLine(Format(config.FeedRate));
                    hasPoint = true;
                }
                else
                {
                    builder.Append("G1 X").Append(Format(x)).Append(" Y").Append(Format(y)).Append(" F").AppendLine(Format(config.FeedRate));
                }
            }

            if (hasPoint)
            {
                builder.Append("G0 Z").AppendLine(Format(config.SafeZ));
            }

            builder.AppendLine("M30");
            return builder.ToString();
        }

        private static string Format(decimal value)
        {
            return value.ToString("0.000", CultureInfo.InvariantCulture);
        }
    }
}
