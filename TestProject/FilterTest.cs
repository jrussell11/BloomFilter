using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using BloomFilter;

namespace TestProject
{
    [TestClass()]
    public class FilterTest
    {
        public TestContext TestContext { get; set; }

        /// <summary>
        /// There should be no false negatives.
        /// </summary>
        [TestMethod()]
        public void NoFalseNegativesTest()
        {
            // set filter properties
            const int capacity = 10000;
            const float errorRate = 0.001F; // 0.1%

            // create input collection
            var inputs = GenerateRandomDataList(capacity);

            // instantiate filter and populate it with the inputs
            var target = new Filter<string>(capacity, errorRate, null);
            foreach (var input in inputs)
            {
                target.Add(input);
            }

            // check for each input. if any are missing, the test failed
            foreach (var input in inputs)
            {
                if (target.Contains(input) == false)
                    Assert.Fail($"False negative: {input}");
            }
        }

        /// <summary>
        /// Only in extreme cases should there be a false positive with this test.
        /// </summary>
        [TestMethod()]
        public void LowProbabilityFalseTest()
        {
            const int capacity = 10000; // we'll actually add only one item
            const float errorRate = 0.0001F; // 0.01%

            // instantiate filter and populate it with a single random value
            var target = new Filter<string>(capacity, errorRate, null);
            target.Add(Guid.NewGuid().ToString());

            // generate a new random value and check for it
            if (target.Contains(Guid.NewGuid().ToString()))
                Assert.Fail("Check for missing item returned true.");
        }

        [TestMethod()]
        public void FalsePositivesInRangeTest()
        {
            // set filter properties
            const int capacity = 1000000;
            const float errorRate = 0.001F; // 0.1%

            // instantiate filter and populate it with random strings
            var target = new Filter<string>(capacity, errorRate, null);
            for (var i = 0; i < capacity; i++)
                target.Add(Guid.NewGuid().ToString());

            // generate new random strings and check for them
            // about errorRate of them should return positive
            var falsePositives = 0;
            const int testIterations = capacity;
            const int expectedFalsePositives = ((int)(testIterations * errorRate)) * 2;
            for (var i = 0; i < testIterations; i++)
            {
                var test = Guid.NewGuid().ToString();
                if (target.Contains(test))
                    falsePositives++;
            }

            if (falsePositives > expectedFalsePositives)
                Assert.Fail($"Number of false positives ({falsePositives}) greater than expected ({expectedFalsePositives}).");
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void OverLargeInputTest()
        {
            // set filter properties
            const int capacity = int.MaxValue - 1;
            const float errorRate = 0.01F; // 1%

            // instantiate filter. this should throw.  
            new Filter<string>(capacity, errorRate, null);
        }

        [TestMethod()]
        public void LargeInputTest()
        {
            // set filter properties
            const int capacity = 2000000;
            const float errorRate = 0.01F; // 1%

            // instantiate filter and populate it with random strings
            var target = new Filter<string>(capacity, errorRate, null);
            for (var i = 0; i < capacity; i++)
                target.Add(Guid.NewGuid().ToString());

            // if it didn't error out on that much input, this test succeeded
        }

        [TestMethod()]
        public void LargeInputTestAutoError()
        {
            // set filter properties
            const int capacity = 2000000;

            // instantiate filter and populate it with random strings
            var target = new Filter<string>(capacity);
            for (var i = 0; i < capacity; i++)
                target.Add(Guid.NewGuid().ToString());

            // if it didn't error out on that much input, this test succeeded
        }

        /// <summary>
        /// If k and m are properly choses for n and the error rate, the filter should be about half full.
        /// </summary>
        [TestMethod()]
        public void TruthinessTest()
        {
            const int capacity = 10000;
            const float errorRate = 0.001F; // 0.1%
            var target = new Filter<string>(capacity, errorRate, null);
            for (var i = 0; i < capacity; i++)
                target.Add(Guid.NewGuid().ToString());

            var actual = target.Truthiness;
            const double expected = 0.5;
            const double threshold = 0.01; // filter shouldn't be < 49% or > 51% "true"
            Assert.IsTrue(Math.Abs(actual - expected) < threshold, $"Information density too high or low. Actual={actual}, Expected={expected}");
        }

        private static List<string> GenerateRandomDataList(int capacity)
        {
            var inputs = new List<string>(capacity);
            for (var i = 0; i < capacity; i++)
            {
                inputs.Add(Guid.NewGuid().ToString());
            }
            return inputs;
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void InvalidCapacityConstructorTest()
        {
            const float errorRate = 0.1F;
            const int capacity = 0; // no good
            // this should throw:
            new Filter<int>(capacity, errorRate, delegate { return 0; });
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void InvalidErrorRateConstructorTest()
        {
            const float errorRate = 10F; // no good
            const int capacity = 10;
            new Filter<int>(capacity, errorRate, delegate { return 0; });
        }
    }
}
