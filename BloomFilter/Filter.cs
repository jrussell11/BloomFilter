using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;

namespace BloomFilter
{
    public class Filter<T>
    {
        /// <summary>
        /// A function that can be used to hash input.
        /// </summary>
        /// <param name="input">The values to be hashed.</param>
        /// <returns>The resulting hash code.</returns>
        public delegate int HashFunction(T input);

        /// <summary>
        /// Creates a new Bloom filter, specifying an error rate of 1/capacity, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// A secondary hash function will be provided for you if your type T is either string or int. Otherwise an exception will be thrown. If you are not using these types please use the overload that supports custom hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        public Filter(int capacity) : this(capacity, null) { }

        /// <summary>
        /// Creates a new Bloom filter, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// A secondary hash function will be provided for you if your type T is either string or int. Otherwise an exception will be thrown. If you are not using these types please use the overload that supports custom hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="errorRate">The accepable false-positive rate (e.g., 0.01F = 1%)</param>
        public Filter(int capacity, int errorRate) : this(capacity, errorRate, null) { }

        /// <summary>
        /// Creates a new Bloom filter, specifying an error rate of 1/capacity, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="hashFunction">The function to hash the input values. Do not use GetHashCode(). If it is null, and T is string or int a hash function will be provided for you.</param>
        public Filter(int capacity, HashFunction hashFunction) : this(capacity, BestErrorRate(capacity), hashFunction) { }

        /// <summary>
        /// Creates a new Bloom filter, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="errorRate">The accepable false-positive rate (e.g., 0.01F = 1%)</param>
        /// <param name="hashFunction">The function to hash the input values. Do not use GetHashCode(). If it is null, and T is string or int a hash function will be provided for you.</param>
        public Filter(int capacity, float errorRate, HashFunction hashFunction) : this(capacity, errorRate, hashFunction, BestM(capacity, errorRate), BestK(capacity, errorRate)) { }

        /// <summary>
        /// Creates a new Bloom filter.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="errorRate">The accepable false-positive rate (e.g., 0.01F = 1%)</param>
        /// <param name="hashFunction">The function to hash the input values. Do not use GetHashCode(). If it is null, and T is string or int a hash function will be provided for you.</param>
        /// <param name="m">The number of elements in the BitArray.</param>
        /// <param name="k">The number of hash functions to use.</param>
        public Filter(int capacity, float errorRate, HashFunction hashFunction, int m, int k)
        {
            // validate the params are in range
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "capacity must be > 0");
            if (errorRate >= 1 || errorRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(errorRate), errorRate, $"errorRate must be between 0 and 1, exclusive. Was {errorRate}");
            if (m < 1) // from overflow in bestM calculation
                throw new ArgumentOutOfRangeException($"The provided capacity and errorRate values would result in an array of length > int.MaxValue. Please reduce either of these values. Capacity: {capacity}, Error rate: {errorRate}");

            // set the secondary hash function
            if (hashFunction == null)
            {
                if (typeof(T) == typeof(String))
                {
                    _secondaryHash = HashString;
                }
                else if (typeof(T) == typeof(int))
                {
                    _secondaryHash = HashInt32;
                }
                else
                {
                    throw new ArgumentNullException(nameof(hashFunction), "Please provide a hash function for your type T, when T is not a string or int.");
                }
            }
            else
                _secondaryHash = hashFunction;

            _hashFunctionCount = k;
            _hashBits = new BitArray(m);
        }

        /// <summary>
        /// Adds a new item to the filter. It cannot be removed.
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            // start flipping bits for each hash of item
            for (var i = 0; i < _hashFunctionCount; i++)
            {
                var hash = ComputeHash(item.GetHashCode(), _secondaryHash(item), i);
                _hashBits[hash] = true;
            }
        }

        /// <summary>
        /// Checks for the existance of the item in the filter for a given probability.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(T item)
        {
            for (var i = 0; i < _hashFunctionCount; i++)
            {
                var hash = ComputeHash(item.GetHashCode(), _secondaryHash(item), i);
                if (_hashBits[hash] == false)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// The ratio of false to true bits in the filter. E.g., 1 true bit in a 10 bit filter means a truthiness of 0.1.
        /// </summary>
        public double Truthiness => (double) TrueBits() / _hashBits.Count;

        private int TrueBits()
        {
            return _hashBits.Cast<bool>().Count(bit => bit);
        }

        /// <summary>
        /// Performs Dillinger and Manolios double hashing. 
        /// </summary>
        private int ComputeHash(int primaryHash, int secondaryHash, int i)
        {
            var resultingHash = (primaryHash + (i * secondaryHash)) % _hashBits.Count;
            return Math.Abs(resultingHash);
        }

        private readonly int _hashFunctionCount;
        private readonly BitArray _hashBits;
        private readonly HashFunction _secondaryHash;

        private static int BestK(int capacity, float errorRate)
        {
            return (int)Math.Round(Math.Log(2.0) * BestM(capacity, errorRate) / capacity);
        }

        private static int BestM(int capacity, float errorRate)
        { 
            return (int)Math.Ceiling(capacity * Math.Log(errorRate, (1.0 / Math.Pow(2, Math.Log(2.0)))));
        }

        private static float BestErrorRate(int capacity)
        {
            var c = (float)(1.0 / capacity);
            if (Math.Abs(c) > 0.0000000001)
                return c;
            return (float)Math.Pow(0.6185, int.MaxValue / capacity); // http://www.cs.princeton.edu/courses/archive/spring02/cs493/lec7.pdf
        }

        /// <summary>
        /// Hashes a 32-bit signed int using Thomas Wang's method v3.1 (http://www.concentric.net/~Ttwang/tech/inthash.htm).
        /// Runtime is suggested to be 11 cycles. 
        /// </summary>
        /// <param name="input">The integer to hash.</param>
        /// <returns>The hashed result.</returns>
        private static int HashInt32(T input)
        {
            var x = input as uint?;
            unchecked
            {
                x = ~x + (x << 15); // x = (x << 15) - x- 1, as (~x) + y is equivalent to y - x - 1 in two's complement representation
                x = x ^ (x >> 12);
                x = x + (x << 2);
                x = x ^ (x >> 4);
                x = x * 2057; // x = (x + (x << 3)) + (x<< 11);
                x = x ^ (x >> 16);
                Debug.Assert(x != null, "x != null");
                return (int)x;
            }
        }

        /// <summary>
        /// Hashes a string using Bob Jenkin's "One At A Time" method from Dr. Dobbs (http://burtleburtle.net/bob/hash/doobs.html).
        /// Runtime is suggested to be 9x+9, where x = input.Length. 
        /// </summary>
        /// <param name="input">The string to hash.</param>
        /// <returns>The hashed result.</returns>
        private static int HashString(T input)
        {
            var s = input as string;
            var hash = 0;

            Debug.Assert(s != null, "s != null");
            foreach (var t in s)
            {
                hash += t;
                hash += (hash << 10);
                hash ^= (hash >> 6);
            }
            hash += (hash << 3);
            hash ^= (hash >> 11);
            hash += (hash << 15);
            return hash;
        }
    }
}