using System.Collections.Generic;
using System.Collections.Generic;

namespace Sand
{
    public static class SoldPicturesRegistry
    {
        private static readonly HashSet<int> _sold = new HashSet<int>();

        public static void MarkSold(int index)
        {
            if (index >= 0) _sold.Add(index);
        }

        public static bool IsSold(int index)
        {
            return index >= 0 && _sold.Contains(index);
        }
    }
}