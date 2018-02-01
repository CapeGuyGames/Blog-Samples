// Copyright Cape Guy Ltd. 2018. http://capeguy.co.uk.
// Provided under the terms of the MIT license -
// http://opensource.org/licenses/MIT. Cape Guy accepts
// no responsibility for any damages, financial or otherwise,
// incurred as a result of using this code.
//
// For more information see https://capeguy.co.uk/2018/02/binary-sorting-for-ilists/.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// Adds helper extension methods to IList<T> for dealing with sorted lists.
/// Implemented using quicksort and binary search algorithms.
/// </summary>
public static class IListExtensions {

	/// <summary>
	/// Swaps the elements at the two given indices within the list.
	/// </summary>
	/// <param name="self">The list we want to swap the elements within.</param>
	/// <param name="index1">The index of the first element we wish to swap.</param>
	/// <param name="index2">The index of the second element we wish to swap.</param>
	/// <typeparam name="T">The type of the elements within the list.</typeparam>
	public static void SwapElements<T> (this IList<T> self, int index1, int index2) {
		T temp = self [index1];
		self [index1] = self [index2];
		self [index2] = temp;
	}

	/// <summary>
	/// Returns the index of the first element in the list for which the given predicate is true.
	/// If no such element exists then returns count.
	/// </summary>
	/// <param name="list"></param>
	/// <param name="predicate"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public static int FirstIndexWhere<T>(this IList<T> list, Func<T, bool> predicate) {
		int count = list.Count;
		for(int i = 0; i < list.Count; ++i) {
			if(predicate(list[i])) {
				return i;
			}
		}

		return count;
	}

	public static IList<T> AsReadOnly<T> (this IList<T> self) {
		if (self is ReadOnlyCollection<T>) {
			return self;
		} else {
			return new ReadOnlyCollection<T>(self);
		}
	}

}