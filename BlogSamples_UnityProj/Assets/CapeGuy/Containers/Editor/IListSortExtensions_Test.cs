// Copyright Cape Guy Ltd. 2018. http://capeguy.co.uk.
// Provided under the terms of the MIT license -
// http://opensource.org/licenses/MIT. Cape Guy accepts
// no responsibility for any damages, financial or otherwise,
// incurred as a result of using this code.
//
// For more information see https://capeguy.co.uk/2018/02/binary-sorting-for-ilists/.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace CapeGuy.Utils.Collections.Test {

	[TestFixture]
	public static class IListSortExtensions_Test {

		static IEnumerable<IList<int>> TestLists () {
			yield return new List<int> ();
			yield return new List<int> {1};
			yield return new List<int> {2, 3};
			yield return new List<int> {3, 2};
			yield return new List<int> {3, 2, 1};
			yield return new List<int> {-3, -2, -1};
			yield return new List<int> {3, 2, 1};

			yield return Enumerable.Range (10, 100).ToList ();
			yield return new List<int> {
				7, 3, 5, 8, 3, 3, 90, -100, 43, 256, 533, 652, 525, 525, 253, 21, 12, 156, 3,
				343, 677, -336, 235, -336, -525, -1, -3, 336, 234, 235, 146, 0, -34,
				234, 345, 345, 345, 674, 667, 453, 526, 635, 67, 757, 667, 345, 345
			};
		}

		static IEnumerable<IList<int>> TestListsSorted () {
			return TestLists ().Select (list => (IList<int>)(list.OrderBy (elem => elem).ToList ()));
		}

		[Test]
		public static void BinarySearchTest () {
			foreach (var sortedList in TestListsSorted ()) {
				var numListElems = sortedList.Count;
				if (numListElems > 0) {
					var minValue = sortedList[0] - 1;
					var maxValue = sortedList[numListElems - 1] + 1;
					Assert.IsTrue (maxValue - minValue <= 2000); // Keep to sensible ranges for test speed.
					for (var currSearchValue = minValue; currSearchValue <= maxValue; ++currSearchValue) {
						int findResult = sortedList.BinarySearch (currSearchValue);
						bool found = findResult >= 0;

						if (found) {
							Assert.IsTrue (sortedList[findResult] == currSearchValue);
						} else {
							Assert.IsFalse (sortedList.Contains (currSearchValue));
							Assert.IsTrue (~findResult == sortedList.BinaryUpperBound (currSearchValue));
						}
					}
				} else {
					Assert.IsTrue(sortedList.BinarySearch (10) < 0);
				}
			}
		}

		[Test]
		public static void BinaryLowerBoundTest () {
			foreach (var sortedList in TestListsSorted ()) {
				var numListElems = sortedList.Count;
				if (numListElems > 0) {
					var minValue = sortedList[0] - 1;
					var maxValue = sortedList[numListElems - 1] + 1;
					Assert.IsTrue (maxValue - minValue <= 2000); // Keep to sensible ranges for test speed.
					for (var currTestValue = minValue; currTestValue <= maxValue; ++currTestValue) {
						int lowerBound = sortedList.BinaryLowerBound (currTestValue);
						int slowLowerBound = sortedList.FirstIndexWhere (elem => elem >= currTestValue);

						Assert.IsTrue (lowerBound == slowLowerBound);
					}
				} else {
					Assert.IsTrue(sortedList.BinaryLowerBound (10) == 0);
				}
			}
		}

		[Test]
		public static void BinaryUpperBoundTest () {
			foreach (var sortedList in TestListsSorted ()) {
				var numListElems = sortedList.Count;
				if (numListElems > 0) {
					var minValue = sortedList[0] - 1;
					var maxValue = sortedList[numListElems - 1] + 1;
					Assert.IsTrue (maxValue - minValue <= 2000); // Keep to sensible ranges for test speed.
					for (var currTestValue = minValue; currTestValue <= maxValue; ++currTestValue) {
						int upperBound = sortedList.BinaryUpperBound (currTestValue);
						int slowUpperBound = sortedList.FirstIndexWhere (elem => elem > currTestValue);

						Assert.IsTrue (upperBound == slowUpperBound);
					}
				} else {
					Assert.IsTrue(sortedList.BinaryUpperBound (10) == 0);
				}
			}
		}

		[Test]
		public static void InsertSortedTest () {
			foreach (var sortedList in TestListsSorted ().Select (list => list.AsReadOnly ())) {
				var numListElems = sortedList.Count;
				if (numListElems > 0) {
					var minValue = sortedList[0] - 1;
					var maxValue = sortedList[numListElems - 1] + 1;
					Assert.IsTrue (maxValue - minValue <= 2000); // Keep to sensible ranges for test speed.
					for (var currTestValue = minValue; currTestValue <= maxValue; ++currTestValue) {
						IList<int> testList = new List<int> (sortedList);
						int insertPos = testList.BinaryInsertSorted (currTestValue);
						Assert.IsTrue (testList[insertPos] == currTestValue);
						Assert.IsTrue (testList.IsSorted ());
						Assert.IsTrue (testList.Count == numListElems + 1);
					}
				} else {
					IList<int> testList = new List<int> (sortedList);
					int insertIndex = testList.BinaryInsertSorted (5);
					Assert.IsTrue (insertIndex == 0);
					Assert.IsTrue (testList.SequenceEqual (new List<int> {5}));
				}
			}
		}

		[Test]
		public static void SortTest () {
			foreach (var currTestList in TestLists ()) {
				var linqSorted = currTestList.OrderBy (elem => elem).ToList ();
					currTestList.Sort();
				Assert.IsTrue (currTestList.SequenceEqual (linqSorted));
			}
		}

		[Test]
		public static void SortedTest () {
			foreach (var currTestList in TestLists ()) {
				var origList = new List<int> (currTestList);
				var linqSorted = currTestList.OrderBy (elem => elem).ToList ();
				Assert.IsTrue (currTestList.Sorted().SequenceEqual (linqSorted));

				// Check the original list hasn't been changed...
				Assert.IsTrue (currTestList.SequenceEqual (origList));
			}
		}

		[Test]
		public static void IsSortedTest () {
			foreach (var currTestList in TestLists ()) {
				var isSorted = currTestList.IsSorted ();
				Assert.IsTrue (currTestList.Sorted ().SequenceEqual (currTestList) == isSorted);
			}
		}

	}

}
