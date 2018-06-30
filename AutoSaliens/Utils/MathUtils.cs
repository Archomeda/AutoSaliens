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
            var index = (int)Math.Ceiling(scale);
            index = index < 0 ? 0 : index >= colors.Count ? colors.Count - 1 : index;
            return colors[index];
        }
    }
}
