using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSIWin_Script_Tool
{
    static class Extensions
    {
        public static byte[] ReadCString(this BinaryReader @this, bool nullTerminated = false)
        {
            List<byte> buffer = new List<byte>();

            for (byte value = @this.ReadByte(); value != 0; value = @this.ReadByte())
            {
                buffer.Add(value);
            }

            if (nullTerminated)
            {
                buffer.Add(0);
            }

            return buffer.ToArray();
        }
    }

    static class ToDictionaryExtentions
    {
        public static IDictionary<TKey, TValue> ToDictionaryEx<TElement, TKey, TValue>(
            this IEnumerable<TElement> source,
            Func<TElement, TKey> keyGetter,
            Func<TElement, TValue> valueGetter)
        {
            IDictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();
            foreach (var e in source)
            {
                var key = keyGetter(e);
                if (dict.ContainsKey(key))
                {
                    continue;
                }

                dict.Add(key, valueGetter(e));
            }
            return dict;
        }
    }
}
