using System;
using System.Collections.Generic;

namespace www.SoLaNoSoft.com.BearChessWin
{
    public static class StringExtensions
    {
        public static bool ContainsCaseInsensitive(this string source, string substring)
        {
            return source?.IndexOf(substring, StringComparison.OrdinalIgnoreCase) > -1;
        }
    }

    public static class EloValueSplitter
    {
        public static int[] GetSplitArray(int minValue, int maxValue, int splitter)
        {
            List<int> allValue = new List<int>();
            int skip = (maxValue - minValue) / splitter;
            for (int i = minValue; i <= maxValue; i += skip)
            {
                allValue.Add(i);
            }
            return allValue.ToArray();
        }

        public static int GetSplitValue(int[] splitArray,  int currentValue)
        {
            for (int i = 0; i < splitArray.Length; i++)
            {
                if (splitArray[i]>=currentValue)
                {
                    return i;
                }
            }
            return 0;
        }
    }
}
