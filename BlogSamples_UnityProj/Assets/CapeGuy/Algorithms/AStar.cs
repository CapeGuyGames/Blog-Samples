// Copyright Cape Guy Ltd. 2015. http://capeguy.co.uk.
// Provided under the terms of the MIT license -
// http://opensource.org/licenses/MIT. Cape Guy accepts
// no responsibility for any damages, financial or otherwise,
// incurred as a result of using this code.
//
// For more information see https://capeguy.co.uk/2016/01/a-for-all/.

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace CapeGuy.Algorithms {

	public class AStar<GraphElement>
	{
		/// Data which defines a single search space in which we can perform AStar searches (defines the actual graph programatically).
		/// Rather than giving all the data up front, we use callback methods to enquire about the graph as we need. That allows for
		/// lazy evaluation of the graph space by the user/algorithm.
		public struct LazyGraph
		{
			public bool GetIsValid ()
			{
				bool isValid = getLowestCostEstimateForMovementBetweenElementsFunc != null;
				isValid = isValid && getElementsConnectedToElementFunc != null;
				isValid = isValid && getActualCostForMovementBetweenElementsFunc != null;
				return isValid;
			}

			// Called repeatedly during AStar calculation to get an estimate of the lowest possible cost of movement between two elements.
			// Note: The second element will always be the current target element.
			public Func<GraphElement, GraphElement, float> getLowestCostEstimateForMovementBetweenElementsFunc;

			// Called repeatedly during AStar calculation to get the elements which are connected to the given elements.
			// Note: IList must not be modified after it is given to the algorithm.
			public Func<GraphElement, IList<GraphElement>> getElementsConnectedToElementFunc;

			// Gets the actual cost of moving from the first element to the second element. This will only be called for pairs of
			// elements returned by getElementsConnectedToElementFunc.
			public Func<GraphElement, GraphElement, float> getActualCostForMovementBetweenElementsFunc;
		}

		/// <summary>
		/// Create an AStar searcher for the given graph.
		///
		/// For more information see https://capeguy.co.uk/2016/01/a-for-all/.
		/// </summary>
		/// <param name="graph">The graph we will be performing searches on.</param>
		public AStar (LazyGraph graph)
		{
			Assert.IsTrue (graph.GetIsValid ());
			_graph = graph;
		}

		/// <summary>
		/// Calculates the shortest route through the graph to the target using the given LazyGraph callbacks.
		/// </summary>
		/// <param name="startingElement">Starting element.</param>
		/// <param name="allowPartialSolution">If true, then the closest to complete solution will be used, even if the solution
		/// doesn't actually reacth the target position.</param>
		public IList<GraphElement> Calculate (GraphElement startingElement, GraphElement targetElement, bool allowPartialSolution = false, float maxAcceptableRouteLength = float.MaxValue)
		{
			Assert.IsTrue (startingElement != null);
			Assert.IsTrue (targetElement != null);
			Assert.IsTrue (0.0f < maxAcceptableRouteLength);

			// Special case for if we are starting our search at the target element.
			if (startingElement.Equals (targetElement)) {
				return new List<GraphElement> { targetElement };
			}

			Assert.IsTrue (_sortedCandidateGraphEntries != null);
			_sortedCandidateGraphEntries.Clear ();

			// If we have a new target entry then we need to clear all our cached data. Otherwise we can reuse it.
			Assert.IsTrue (_graphEntries != null);
			if (_targetEntry == null || !targetElement.Equals(_targetEntry.graphElement) || _maxAcceptableRouteLength != maxAcceptableRouteLength) {
				_targetEntry = null;
				_graphEntries.Clear ();
				_maxAcceptableRouteLength = maxAcceptableRouteLength;
			}

			if (_targetEntry == null) {
				_targetEntry = CreateGraphEntryFor (targetElement);
			}
			Assert.IsTrue (_targetEntry.graphElement.Equals(targetElement));
			AddStartGraphEntry (startingElement);

			while (true)
			{
				if (_sortedCandidateGraphEntries.Count == 0)
				{
					// We're run out of candidates and haven't found the solution.
					// There is no route across the graph. :-(.
					break;
				}
				else
				{
					GraphEntry currTestEntry = GetCurrBestTestEntry();

					if (currTestEntry.Equals(targetElement))
					{
						// We have a full best solution to the target.
						break;
					}
					else
					{
						ProcessBestEntry(currTestEntry);
					}
				}
			}

			List<GraphElement> solutionList = ExtractSolution(targetElement, allowPartialSolution);
			Assert.IsTrue (solutionList == null || startingElement.Equals (solutionList[0]));

			// Reset the working data.
			_sortedCandidateGraphEntries = new List<GraphEntry>();
			_graphEntries = new Dictionary<GraphElement, GraphEntry>();

			return solutionList;
		}

		#region private

		private void AddStartGraphEntry (GraphElement startingElement)
		{
			Assert.IsTrue (_targetEntry != null);
			if (_startEntry == null || !_startEntry.graphElement.Equals(startingElement)) {
				_startEntry = GetEntryForElement (startingElement);
				if (_startEntry == null) {
					_startEntry = CreateGraphEntryFor (startingElement);
				}
			}
			AddSortedCandidate (_startEntry);
		}

		private void ProcessBestEntry (GraphEntry bestEntry)
		{
			// First we should remove the entry from the candidate array
			Assert.IsTrue (_sortedCandidateGraphEntries [0] == bestEntry);
			_sortedCandidateGraphEntries.RemoveAt (0);
			GraphElement bestElement = bestEntry.graphElement;
			Assert.IsTrue (bestElement != null);

			// Now we should add/update all the connected entries.
			if (bestEntry.connectedElements != null) {
				foreach (GraphElement currConnectedElement in bestEntry.connectedElements)
				{
					float connectionCost = _graph.getActualCostForMovementBetweenElementsFunc(bestElement, currConnectedElement);
					float totalCostToConnection = bestEntry.bestCostToHere + connectionCost;
					GraphEntry connectedEntry = GetEntryForElement(currConnectedElement);
					if (connectedEntry == _startEntry)
					{
						// This is the start entry so just ignore it as the best route never returns to the start point!
					} else if (connectedEntry == _targetEntry) {
						// We've reached the target entry, add it as a sorted candidate, we're only finished if this route becomes the best possible.
						if (connectedEntry.bestCostVia == null || totalCostToConnection < connectedEntry.bestCostToHere) {
							connectedEntry.SetBestCost(bestEntry, totalCostToConnection);
							AddSortedCandidate(connectedEntry);
						}
					}
					else if (connectedEntry != null && connectedEntry.bestCostVia != null)
					{
						// We've already visited this entry so see if this is the optimum route.
						if (totalCostToConnection < connectedEntry.bestCostToHere)
						{
							// This route is shorter than the one we've previously found so replace it.
							RemoveSortedCandidate(connectedEntry);
							connectedEntry.SetBestCost(bestEntry, totalCostToConnection);
							AddSortedCandidate(connectedEntry);
						}
					}
					else
					{
						// This is the first time we've seen this entry so just add it
						Assert.IsTrue (connectedEntry == null || connectedEntry.bestCostVia == null);
						if (connectedEntry == null)
						{
							connectedEntry = CreateGraphEntryFor(currConnectedElement);
						}
						connectedEntry.SetBestCost(bestEntry, totalCostToConnection);
						AddSortedCandidate(connectedEntry);
					}
				}
			}
		}

		private GraphEntry CreateGraphEntryFor (GraphElement graphElement)
		{
			Assert.IsTrue (!_graphEntries.ContainsKey(graphElement)); // We should only be creating a new element if there isn't one already.
			// If the _targetEntry hasn't been setup yet then we must be building it now. Pass null (which evaluates to zero estimated cost as we ARE the target).
			GraphEntry newEntry = new GraphEntry (graphElement, _targetEntry != null ? _targetEntry.graphElement : graphElement, _graph);
			_graphEntries [graphElement] = newEntry;
			return newEntry;
		}

		private GraphEntry GetCurrBestTestEntry ()
		{
			Assert.IsTrue (_sortedCandidateGraphEntries.Count > 0);
			return _sortedCandidateGraphEntries [0];
		}

		/// <summary>
		/// Extracts a list of GraphElements representing the shortest route from start to finish.
		/// Returns null if no such route was found.
		/// </summary>
		/// <returns>The solution.</returns>
		/// <param name="targetElement">Target element.</param>
		private List<GraphElement> ExtractSolution (GraphElement targetElement, bool allowPartialSolution)
		{
			List<GraphElement> solutionList = null;

			GraphEntry currEntry = GetEntryForElement (targetElement);
			bool hasCompleteSolution = currEntry != null && currEntry.bestCostVia != null;
			if (!hasCompleteSolution && allowPartialSolution) {
				// Get a list of all the entries which we did reach in our search.
				var viableEntries = _graphEntries.Values.Where (reachedFilterEntry => (reachedFilterEntry.bestCostVia != null || reachedFilterEntry == _startEntry));
				if (!(viableEntries.Count() == 0)) {
					// Filter the list down to the entries which got the closest possible distance away from the target...
					var orderedViableEntries = viableEntries.OrderBy (distFromSolutionEntry => distFromSolutionEntry.bestPossibleCostToTarget);
					float bestPossibleRemainingCost = orderedViableEntries.First ().bestPossibleCostToTarget;
					viableEntries = viableEntries.Where (filderRemaininCostEntry => filderRemaininCostEntry.bestPossibleCostToTarget == bestPossibleRemainingCost);
					Assert.IsFalse (viableEntries.Count() == 0);

					// Order the remaining viable entries by the total cost and pick the smallest...
					orderedViableEntries = viableEntries.OrderBy (totalCostOrderEntry => totalCostOrderEntry.currentBestPossibleTotalCost);
					Assert.IsFalse (orderedViableEntries.Count() == 0);

					// Set the curr entry to be the best possible end entry...
					currEntry = orderedViableEntries.First ();
				}
			}

			// Now we know where the path ends, trace the route back to the start entry.
			if (currEntry != null && (currEntry.bestCostVia != null || currEntry == _startEntry))
			{
				solutionList = new List<GraphElement> ();
				while (currEntry != null)
				{
					solutionList.Add (currEntry.graphElement);
					currEntry = currEntry.bestCostVia;
				}
				solutionList.Reverse ();
			}

			return solutionList;
		}

		/// <summary>
		/// Tries to get the associated entry for the given graph element, returns null if no such element exists.
		/// </summary>
		/// <returns>The entry for element.</returns>
		/// <param name="graphElement">Graph element.</param>
		private GraphEntry GetEntryForElement (GraphElement graphElement)
		{
			GraphEntry graphEntry;
			_graphEntries.TryGetValue (graphElement, out graphEntry);
			return graphEntry;
		}

		// Helper for AddSortedCandidate which orders the candidates, putting the least cost first.
		private class CandidateComparer : IComparer<GraphEntry>
		{
			public int Compare (GraphEntry lhs, GraphEntry rhs)
			{
				// Sort firstly by best possible total cost...
				if (lhs.currentBestPossibleTotalCost < rhs.currentBestPossibleTotalCost)
				{
					return -1;
				}
				else if (lhs.currentBestPossibleTotalCost == rhs.currentBestPossibleTotalCost)
				{
					// Within equal best possible total costs, sort the ones we have gone furthest with first.
					if (lhs.bestCostToHere > rhs.bestCostToHere) {
						return -1;
					} else if (lhs.bestCostToHere < rhs.bestCostToHere) {
						return 1;
					} else {
						return 0;
					}
				}
				else
				{
					return 1;
				}
			}
		}

		private void AddSortedCandidate (GraphEntry newCandidateEntry)
		{
			Assert.IsTrue (!_sortedCandidateGraphEntries.Contains (newCandidateEntry));

			// Only actually add if if it is possible that it will produce a better solution than our existing best solution, and that it's better than the worst cost we're interested in.
			float currBestSolutionCost = _targetEntry.currentBestPossibleTotalCost;
			if (newCandidateEntry.currentBestPossibleTotalCost <= _maxAcceptableRouteLength && (_targetEntry.bestCostVia == null || newCandidateEntry.currentBestPossibleTotalCost < currBestSolutionCost))
			{
				int findIndex = _sortedCandidateGraphEntries.BinarySearch (newCandidateEntry, new CandidateComparer ());
				if (findIndex < 0) {
					// We didn't find it so insert it at the bitwise compliment of findIndex (see https://msdn.microsoft.com/en-us/library/w4e7fxsh(v=vs.110).aspx)
					_sortedCandidateGraphEntries.Insert (~findIndex, newCandidateEntry);
				} else {
					_sortedCandidateGraphEntries.Insert(findIndex, newCandidateEntry);
				}
			}
		}

		private void RemoveSortedCandidate (GraphEntry candidateEntry)
		{
			Assert.IsTrue (candidateEntry != null);
			CandidateComparer comparer = new CandidateComparer ();
			int findIndex = _sortedCandidateGraphEntries.BinarySearch (candidateEntry, comparer);
			if (findIndex >= 0)
			{
				// Find the lower bound...
				int lowerBound = findIndex;
				while (lowerBound > 0) {
					if (comparer.Compare (_sortedCandidateGraphEntries[lowerBound - 1], _sortedCandidateGraphEntries[findIndex]) == 0) {
						--lowerBound;
					} else {
						break;
					}
				}

				// Find the upper bound...
				int upperBound = findIndex;
				while (upperBound < (_sortedCandidateGraphEntries.Count - 1)) {
					if (comparer.Compare (_sortedCandidateGraphEntries[upperBound + 1], _sortedCandidateGraphEntries[findIndex]) == 0) {
						++upperBound;
					} else {
						break;
					}
				}

				// Loop over all possible candidates until we find the one we're looking for.
				for (int currTestIndex = lowerBound; currTestIndex <= upperBound; ++currTestIndex) {
					Assert.IsTrue (comparer.Compare (_sortedCandidateGraphEntries[currTestIndex], _sortedCandidateGraphEntries[findIndex]) == 0);
					if (_sortedCandidateGraphEntries[findIndex] == candidateEntry) {
						_sortedCandidateGraphEntries.RemoveAt(findIndex);
						break;
					}
				}
			}
		}

		private class GraphEntry
		{
			public GraphEntry (GraphElement graphElement, GraphElement targetElement, LazyGraph graph)
			{
				m_graphElement = graphElement;
				m_bestPossibleCostToTarget = targetElement != null ? graph.getLowestCostEstimateForMovementBetweenElementsFunc (graphElement, targetElement) : 0.0f;
				IList<GraphElement> connectedElements = graph.getElementsConnectedToElementFunc (graphElement);
				m_connectedElements = connectedElements != null ? connectedElements : null;
			}

			public void SetBestCost (GraphEntry viaEntry, float totalCostToHere)
			{
				Assert.IsTrue (totalCostToHere < this.bestCostToHere || bestCostVia == null);
				Assert.IsTrue (totalCostToHere >= 0.0f);
				Assert.IsTrue (viaEntry != null);
				Assert.IsTrue (totalCostToHere >= viaEntry.bestCostToHere); // We can't have a smaller cost than our via entry.

				this.m_bestCostToHere = totalCostToHere;
				this.m_bestCostVia = viaEntry;
			}

			public float currentBestPossibleTotalCost { get { return bestCostToHere + bestPossibleCostToTarget; } }

			public float bestCostToHere { get { return m_bestCostToHere; } }
			public GraphEntry bestCostVia { get { return m_bestCostVia; } }

			public GraphElement graphElement { get { return m_graphElement; } }
			public float bestPossibleCostToTarget { get { return m_bestPossibleCostToTarget; } }
			public IList<GraphElement> connectedElements { get { return m_connectedElements; } }

			#region private
			// Can only be set on initialisation.
			private GraphElement m_graphElement;
			private float m_bestPossibleCostToTarget;
			private IList<GraphElement> m_connectedElements;

			// Can be updated (via SetBestCost
			public float m_bestCostToHere = 0.0f;
			public GraphEntry m_bestCostVia = null;
			#endregion
		}

		private LazyGraph _graph;

		// Contains the current candidates in sorted order (best first).  Once processed, a candidate should be removed from the candidate list.
		private List<GraphEntry> _sortedCandidateGraphEntries = new List<GraphEntry>();

		// Contains all the graph entries, indexed by GraphElement.
		private Dictionary<GraphElement, GraphEntry> _graphEntries = new Dictionary<GraphElement, GraphEntry>();

		// The current target element.
		GraphEntry _targetEntry;

		// The current start element.
		GraphEntry _startEntry;

		float _maxAcceptableRouteLength = float.MaxValue;
	}

	#endregion

}