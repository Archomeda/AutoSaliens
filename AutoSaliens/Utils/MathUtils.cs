using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoSaliens.Utils
{
    internal static class MathUtils
    {
        public static string ScaleColor(double value, double maxValue, IList<string> colors)
        {
            if (maxValue == 0)
                return colors.Last();

            var scale = (colors.Count - 1) * value / maxValue;
            return colors[(int)Math.Ceiling(scale)];
        }
    }
}
