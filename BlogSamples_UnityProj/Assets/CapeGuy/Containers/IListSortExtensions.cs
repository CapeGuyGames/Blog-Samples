// Copyright Cape Guy Ltd. 2018. http://capeguy.co.uk.
// Provided under the terms of the MIT license -
// http://opensource.org/licenses/MIT. Cape Guy accepts
// no responsibility for any damages, financial or otherwise,
// incurred as a result of using this code.
//
// For more information see https://capeguy.co.uk/2018/02/binary-sorting-for-ilists/.

using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

/// <summary>
/// Adds helper extension methods to IList<T> for dealing with sorted lists.
/// Implemented using quicksort and binary search algorithms.
/// Note: This script also requires IListExtensions.
/// </summary>
public static class IListSortExtensions {

	// Binary sorting and searching for IList...

	/// <summary>
	/// Inserts the given element into the given sorted list.
	/// Note: Assumes the list is sorted by the default comparer for type T.
	/// </summary>
	/// <returns>The index at which the element was inserted.</returns>
	public static int BinaryInsertSorted<T> (this IList<T> self, T element) {
		return BinaryInsertSorted (self, element, DefaultSortPred<T>.sortPred);
	}

	/// <summary>
	/// Inserts the given element into the given sorted list.
	/// Note: Assumes the list is sorted by the given comparer.
	/// </summary>
	/// <returns>The index at which the element was inserted.</returns>
	public static int BinaryInsertSorted<T> (this IList<T> self, T element, IComparer<T> comparer) {
		return BinaryInsertSorted (self, element, comparer.Compare);
	}

	/// <summary>
	/// Inserts the given element into the given sorted list.
	/// Note: Assumes the list is sorted by the given sortPred.
	/// </summary>
	/// <returns>The index at which the element was inserted.</returns>
	public static int BinaryInsertSorted<T> (this IList<T> self, T element, Func<T, T, int> sortPred) {
		var insertPos = BinaryLowerBound (self, element, sortPred);
		self.Insert (insertPos, element);
		return insertPos;
	}

	/// <summary>
	/// Returns the index of the first element in the sorted list which is greater than or equal to the given element.
	/// Note: Assumes the list is sorted by the given default comparer for type T.
	/// Note: If the list contains no elements which are greater than or equal to the given element
	/// then self.Count will be returned. It is always safe to us List.InsertAt directly with the result.
	/// </summary>
	public static int BinaryLowerBound<T> (this IList<T> self, T testElement) {
		return BinaryLowerBound (self, testElement, DefaultSortPred<T>.sortPred);
	}

	/// <summary>
	/// Returns the index of the first element in the sorted list which is greater than or equal to the given element.
	/// Note: Assumes the list is sorted by the given comparer.
	/// Note: If the list contains no elements which are greater than or equal to the given element
	/// then self.Count will be returned. It is always safe to us List.InsertAt directly with the result.
	/// </summary>
	public static int BinaryLowerBound<T> (this IList<T> self, T testElement, IComparer<T> comparer) {
		return BinaryLowerBound (self, testElement, comparer.Compare);
	}

	/// <summary>
	/// Returns the index of the first element in the sorted list which is greater than or equal to the given element.
	/// Note: Assumes the list is sorted by the given sortPred.
	/// Note: If the list contains no elements which are greater than or equal to the given element
	/// then self.Count will be returned. It is always safe to us List.InsertAt directly with the result.
	/// </summary>
	public static int BinaryLowerBound<T, U> (this IList<T> self, U testElement, Func<T, U, int> sortPred) {
		return BinaryFindFirst (self, testElem => sortPred (testElem, testElement) >= 0);
	}

	/// <summary>
	/// Returns the index of the first element in the sorted list which is greater the given element.
	/// Note: Assumes the list is sorted by the default comparer for type T.
	/// Note: If the list contains no elements which are greater than the given element
	/// then self.Count will be returned. It is always safe to us List.InsertAt directly with the result.
	/// </summary>
	public static int BinaryUpperBound<T> (this IList<T> self, T testElement) {
		return BinaryUpperBound (self, testElement, DefaultSortPred<T>.sortPred);
	}

	/// <summary>
	/// Returns the index of the first element in the sorted list which is greater the given element.
	/// Note: Assumes the list is sorted by the given comparer.
	/// Note: If the list contains no elements which are greater than the given element
	/// then self.Count will be returned. It is always safe to us List.InsertAt directly with the result.
	/// </summary>
	public static int BinaryUpperBound<T> (this IList<T> self, T testElement, IComparer<T> comparer) {
		return BinaryUpperBound (self, testElement, comparer.Compare);
	}

	/// <summary>
	/// Returns the index of the first element in the sorted list which is greater the given element.
	/// Note: Assumes the list is sorted by the given sortPred.
	/// Note: If the list contains no elements which are greater than the given element
	/// then self.Count will be returned. It is always safe to us List.InsertAt directly with the result.
	/// </summary>
	public static int BinaryUpperBound<T, U> (this IList<T> self, U testElement, Func<T, U, int> sortPred) {
		return BinaryFindFirst (self, testElem => sortPred (testElem, testElement) > 0);
	}

	/// <summary>
	/// Returns the index of the last element in the sorted list for which the given predicate is true.
	/// Note: Assumes the list is sorted wrt the given predicate.
	/// ie. the predicate has at most one switch from true to false for the list.
	/// Note: If the list contains no elements for which the predicate is true then -1 will be returned.
	/// </summary>
	public static int BinaryFindLast<T> (this IList<T> self, Func<T, bool> predicate) {
		var firstFalse = BinaryFindFirst (self, elem => !predicate (elem));
		return firstFalse - 1;
	}

	/// <summary>
	/// Returns the index of the last element in the sorted list for which the given predicate is true.
	/// Note: Assumes the list is sorted wrt the given predicate where the given userData is passed to
	/// each call of the predicate.
	/// ie. the predicate has at most one switch from false to true for the list.
	/// Using this version over just a predicate allows usage of BinaryFindLast which avoids closures
	/// in the predicate (and the associated memory allocation involved with that).
	/// Note: If the list contains no elements for which the predicate is true then -1 will be returned.
	/// </summary>
	public static int BinaryFindLast<T, U> (this IList<T> self, Func<T, U, bool> predicate, U userData) {
		var firstFalse = BinaryFindFirst (self, (elem, userDataInner) => !predicate (elem, userDataInner), userData);
		return firstFalse - 1;
	}

	/// <summary>
	/// Returns the index of the first element in the sorted list for which the given predicate is true.
	/// Note: Assumes the list is sorted wrt the given predicate.
	/// ie. the predicate has at most one switch from false to true for the list.
	/// Note: If the list contains no elements for which the predicate is true then self.Count will be returned.
	/// It is always safe to use List.InsertAt directly with the result.
	/// </summary>
	public static int BinaryFindFirst<T> (this IList<T> self, Func<T, bool> predicate) {
		int numElements = self.Count;
		int lowerIndex = 0;
		int upperIndex = numElements;

		while (lowerIndex < upperIndex) {
			int middleIndex = lowerIndex + ((upperIndex - lowerIndex) / 2);
			if (predicate (self[middleIndex])) {
				upperIndex = middleIndex;
			} else {
				lowerIndex = middleIndex + 1;
			}
		}
		Assert.IsTrue (lowerIndex >= upperIndex);
		Assert.IsTrue (0 <= lowerIndex && lowerIndex <= numElements);
		return lowerIndex;
	}

	/// <summary>
	/// Returns the index of the first element in the sorted list for which the given predicate is true.
	/// Note: Assumes the list is sorted wrt the given predicate where the given userData is passed to
	/// each call of the predicate.
	/// ie. the predicate has at most one switch from false to true for the list.
	/// Using this version over just a predicate allows usage of BinaryFindFirst which avoids closures
	/// in the predicate (and the associated memory allocation involved with that).
	/// Note: If the list contains no elements for which the predicate is true then self.Count will be returned.
	/// It is always safe to use List.InsertAt directly with the result.
	/// </summary>
	public static int BinaryFindFirst<T, U> (this IList<T> self, Func<T, U, bool> predicate, U userData) {
		int numElements = self.Count;
		int lowerIndex = 0;
		int upperIndex = numElements;

		while (lowerIndex < upperIndex) {
			int middleIndex = lowerIndex + ((upperIndex - lowerIndex) / 2);
			if (predicate (self[middleIndex], userData)) {
				upperIndex = middleIndex;
			} else {
				lowerIndex = middleIndex + 1;
			}
		}
		Assert.IsTrue (lowerIndex >= upperIndex);
		Assert.IsTrue (0 <= lowerIndex && lowerIndex <= numElements);
		return lowerIndex;
	}

	/// <summary>
	/// Searches the entire sorted List<T> for an element.
	/// Note: Assumes the list is sorted by the default comparer for type T.
	/// </summary>
	/// <returns>The index of item in the sorted IList<T>, if item is found;
	/// otherwise, a negative number that is the bitwise complement of the index
	/// of the next element that is larger than item or, if there is no larger element,
	/// the bitwise complement of Count.</returns>
	public static int BinarySearch<T> (this IList<T> self, T searchElement) {
		return BinarySearch (self, searchElement, DefaultSortPred<T>.sortPred);
	}

	/// <summary>
	/// Searches the entire sorted List<T> for an element.
	/// Note: Assumes the list is sorted by the given comparer.
	/// </summary>
	/// <returns>The index of item in the sorted IList<T>, if item is found;
	/// otherwise, a negative number that is the bitwise complement of the index
	/// of the next element that is larger than item or, if there is no larger element,
	/// the bitwise complement of Count.</returns>
	public static int BinarySearch<T> (this IList<T> self, T searchElement, IComparer<T> comparer) {
		return BinarySearch (self, searchElement, comparer.Compare);
	}

	/// <summary>
	/// Searches the entire sorted List<T> for an element.
	/// /// Note: Assumes the list is sorted by the given sortPred.
	/// </summary>
	/// <returns>The index of item in the sorted IList<T>, if item is found;
	/// otherwise, a negative number that is the bitwise complement of the index
	/// of the next element that is larger than item or, if there is no larger element,
	/// the bitwise complement of Count.</returns>
	public static int BinarySearch<T, U> (this IList<T> self, U element, Func<T, U, int> sortPred) {
		int numElems = self.Count;
		int lowerIndex = self.BinaryLowerBound (element, sortPred);
		if (lowerIndex < numElems && sortPred (self[lowerIndex], element) == 0) {
			return lowerIndex;
		}
		return ~lowerIndex;
	}

	/// <summary>
	/// Calculates if the list is sorted by the default comparer for type T.
	/// </summary>
	public static bool IsSorted<T> (this IList<T> self) {
		return IsSorted (self, DefaultSortPred<T>.sortPred);
	}

	/// <summary>
	/// Calculates if the list is sorted by the given comparer.
	/// </summary>
	public static bool IsSorted<T> (this IList<T> self, IComparer<T> comparer) {
		return IsSorted (self, comparer.Compare);
	}

	/// <summary>
	/// Calculates if the list is sorted by the given sortPred.
	/// </summary>
	public static bool IsSorted<T> (this IList<T> self, Func<T, T, int> sortPred) {
		var numElems = self.Count;
		for (var i = 1; i < numElems; ++i) {
			if (sortPred(self[i - 1],self[i]) > 0) {
				return false;
			}
		}
		return true;
	}

	/// <summary>
	/// Creates a sorted copy of the given list using the default comparer for type T.
	/// </summary>
	public static IList<T> Sorted<T> (this IList<T> self) {
		return Sorted (self, DefaultSortPred<T>.sortPred);
	}

	/// <summary>
	/// Creates a sorted copy of the given list using the given comparer.
	/// </summary>
	public static IList<T> Sorted<T> (this IList<T> self, IComparer<T> comparer) {
		return Sorted (self, comparer.Compare);
	}

	/// <summary>
	/// Creates a sorted copy of the given list using the given sortPred.
	/// </summary>
	public static IList<T> Sorted<T> (this IList<T> self, Func<T, T, int> sortPred) {
		var copy = new List<T> (self);
		copy.Sort ();
		return copy;
	}

	/// <summary>
	/// Sorts the list using the default comparer for type T.
	/// </summary>
	public static void Sort<T> (this IList<T> self) {
		Sort(self, DefaultSortPred<T>.sortPred);
	}

	/// <summary>
	/// Sorts the list using the given comparer.
	/// </summary>
	public static void Sort<T> (this IList<T> self, IComparer<T> comparer) {
		Sort(self, comparer.Compare);
	}

	/// <summary>
	/// Sorts the list using the given sortPred
	/// </summary>
	public static void Sort<T> (this IList<T> self, Func<T, T, int> sortPred) {
		Sort(self, 0, self.Count - 1, sortPred);
	}

	#region Sort Implementation

	struct DefaultSortPred<T> {
		public static readonly IComparer<T> comparer = System.Collections.Generic.Comparer<T>.Default;
		public static readonly Func<T, T, int> sortPred = (lhs, rhs) => comparer.Compare (lhs, rhs);
	}

	const int insertionSortThreshold = 10;
	static void Sort<T> (IList<T> list, int lowIndex, int highIndex, Func<T, T, int> sortPred) {
		if (highIndex - lowIndex >= insertionSortThreshold) {
			Quicksort (list, lowIndex, highIndex, sortPred);
		} else {
			InsertionSort (list, lowIndex, highIndex, sortPred);
		}
	}

	static void Quicksort<T> (IList<T> list, int lowIndex, int highIndex, Func<T, T, int> sortPred) {
		if (lowIndex < highIndex) {
			int leftIndex, rightIndex;
			QuicksortPartition (list, lowIndex, highIndex, sortPred, out leftIndex, out rightIndex);
			var leftHighIndex = leftIndex - 1;
			var rightLowIndex = rightIndex + 1;
			Sort (list, lowIndex, leftHighIndex, sortPred);
			Sort (list, rightLowIndex, highIndex, sortPred);
		}
	}

	/// <summary>
	/// Partition the list section from lowIndex to highIndex into three parts
	/// 1. The elements less than pivot in the low section
	/// 2. The elements greater than pivot in the high section
	/// 3. The elements equal to pivot in the middle section
	/// </summary>
	/// <param name="list">The list we are partitioning part of</param>
	/// <param name="lowIndex">The first index of the partition</param>
	/// <param name="highIndex">The last index of the partition</param>
	/// <param name="sortPred">The predicate we are sorting the list WRT</param>
	/// <param name="leftIndex">The lowest index of an element greater than or equal to the pivot</param>
	/// <param name="rightIndex">The highest index of an element less than or equal to the pivot</param>
	/// <typeparam name="T"></typeparam>
	static void QuicksortPartition<T> (
		IList<T> list,
		int lowIndex,
		int highIndex,
		Func<T, T, int> sortPred,
		out int leftIndex,
		out int rightIndex
	) {
		var pivotIndex = lowIndex + ((highIndex - lowIndex) / 2);
		var pivotElem = list[pivotIndex];
		Assert.IsTrue(sortPred(pivotElem, pivotElem) == 0); // The pivot element should equal itself!

		// Move the pivot to the start of the list (as we already know it's less than or equal to the pivot)
		list.SwapElements (lowIndex, pivotIndex);

		var l = lowIndex; // The first index where the element is not known to be less than the pivot element
		var h = highIndex; // The last element where it is not known to be greater than the pivot element.
		var m = Math.Min(l+1, h); // The first element where the element is not known to be less than or equal to the pivot element

		while (m <= h) {
			var mResult = sortPred(list[m], pivotElem);
			if (mResult < 0) {
				list.SwapElements (l, m);
				++l;
				++m;
			} else if (mResult > 0) {
				list.SwapElements (h, m);
				--h;
			} else {
				++m;
			}
		}

		leftIndex = l;
		rightIndex = h;
	}

	static void InsertionSort<T> (IList<T> list, int lowIndex, int highIndex, Func<T, T, int> sortPred) {
		// Find the lowest element, put it at the bottom and recurse...
		if (lowIndex < highIndex) {
			var lowestElemIndex = lowIndex;
			var lowestElem = list[lowestElemIndex];
			for (int currIndex = lowIndex + 1; currIndex <= highIndex; ++currIndex) {
				var currElem = list[currIndex];
				if (sortPred (lowestElem, currElem) >= 0) {
					lowestElemIndex = currIndex;
					lowestElem = currElem;
				}
			}

			if (lowestElemIndex != lowIndex) {
				list.SwapElements (lowestElemIndex, lowIndex);
			}
			InsertionSort (list, lowIndex + 1, highIndex, sortPred);
		}
	}

	#endregion
}
