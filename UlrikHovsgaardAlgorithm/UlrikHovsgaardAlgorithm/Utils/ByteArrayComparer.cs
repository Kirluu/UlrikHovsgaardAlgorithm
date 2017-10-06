using System;
using System.Collections.Generic;

namespace UlrikHovsgaardAlgorithm.Utils
{
    /// <summary>
    /// http://stackoverflow.com/questions/1440392/use-byte-as-key-in-dictionary
    /// </summary>
    public class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[] left, byte[] right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }
            if (left.Length != right.Length)
            {
                return false;
            }
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }
            return true;
        }
        public int GetHashCode(byte[] array)
        {
            if (array == null)
                throw new ArgumentNullException("'array' parameter is null.");
            //int sum = 0;
            //foreach (byte cur in array)
            //{
            //    sum += cur;
            //}
            //return sum;
            unchecked
            {
                int hash = 17;
                foreach (byte element in array)
                {
                    hash = hash * 31 + element.GetHashCode();
                }
                return hash;
            }
        }
    }
}
