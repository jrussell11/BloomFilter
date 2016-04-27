A Bloom filter is a data structure optimized for fast, space-efficient set membership tests. Bloom filters have the unusual property of requiring constant time to add an element to the set or test for membership, regardless of the size of the elements or the number of elements already in the set. No other constant-space set data structure has this property.

It works by storing a bit vector representing the set S' = {h[i](x) | x in S, i = 1, …, k}, where h[1], …, h[k] := {0, 1} -> [n lg(1/ε) lg e] are hash functions. Additions are simply setting k bits to 1, specifically those at h[1](x), …, h[k](x). Checks are implemented by performing those same hash functions and returning true if all of the resulting positions are 1.

Because the set stored is a proper superset of the set of items added, false positives may occur, though false negatives cannot. The false positive rate can be specified.

Bloom filters offer the following advantages:

	Space: Approximately n * lg(1/ε), where ε is the false positive rate and n is the number of elements in the set.
		Example: There are approximately 170k words in the English language. If we consider that to be our set (therefore n = 1.7E5), and we wish to search a corpus for them with a 1% false positive rate, the filter would require about (1.7E5 * lg(1 / 0.01)) ≈ 162 KB. Contrast this with a hashtable, which would require (1.7E5 elements * 32 bits per element) ≈ 664 KB. Obviously explicit string storage would be significantly more. 
	Precision: Arbitrary precision, where increasing precision requires more space (following the above size equation) but not more time.
		Example: If we wanted to reduce our false positive rate in the above example from one percent to one permille the space requirement would go from about 162 KB to about 207 KB. 
	Time: O(k) where k is the number of hash functions. The optimal number of hash functions (though a different number can be supplied by the user if desired) is ceiling(lg(1/ε))
		Example: In keeping with our above example, if the accepted false positive rate is 0.001, k = 10. 

A real world application illustrating the size difference might be Google Chrome's malicious URL detection. With a set of around 1 million URLs, each URL hashing down to about 25 bytes, results in a set of around 25 MB. This is significantly larger than Chrome itself. Instead using a Bloom filter with a 1% error rate requires only 1.13 MB. 

This implementation uses Dillinger & Manolios double hashing to provide all but the first two hash functions. By default the first hash function is the type's GetHashCode() method. This implementation also includes default secondary hash functions for strings (Jenkin's "One at a time" method - see http://www.burtleburtle.net/bob/hash/doobs.html) and integers (Wang's method - see http://burtleburtle.net/bob/hash/integer.html).

Bloom filters are due to Burton H. Bloom, as described in the Communications of the ACM in July 1970. The full paper is available at http://portal.acm.org/citation.cfm?doid=362686.362692. 

Usage is straightforward for the most common cases: strings and ints. The only thing you need to know is approximately how many items you'll be adding. 

Here's an example demonstrating its use with strings:

int capacity = 2000000; // the number of items you expect to add to the filter
var filter = new Filter<string>(capacity);
filter.Add("SomeString"); // add your items
if (filter.Contains("SomeString")) // check other strings against the filter
	Console.WriteLine("Match!");

Bloom filters can't be resized, so setting the capacity is important for memory sizing. The false-positive rate also plays in here. 

If you don't specify false-positive rate you will get 1 / capacity, unless that is too small in which case you will get 0.6185^(int.MaxValue / capacity), which is nearly optimal

If you are going to be working with some other type T you will have to provide your own hash algorithm that takes a T and returns an int. Do NOT use the BCL's GetHashCode method. If you end up creating one for a common type (e.g., CRC for files) please add it to the source so that others may make use of your work.

Overloads are provided for specifying your own error rate, hash function, array size, double hash function count, etc. 