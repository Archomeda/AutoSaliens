using System;
using System.Collections.Generic;
using System.Threading;

namespace AutoSaliens.Utils
{
    internal static class ListExtensions
    {
        // https://stackoverflow.com/a/1262619

        [ThreadStatic] private static Random Local;

        private static Random ThisThreadsRandom =>
            Local ?? (Local = new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId)));


        public static IList<T> Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThisThreadsRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
            return list;
        }
    }
}
