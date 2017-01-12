// Copyright Cape Guy Ltd. 2015. http://capeguy.co.uk.
// Provided under the terms of the MIT license -
// http://opensource.org/licenses/MIT. Cape Guy accepts
// no responsibility for any damages, financial or otherwise,
// incurred as a result of using this code.
//
// For more information see https://capeguy.co.uk/2016/01/a-for-all/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// Tests for CapeGuy.Algorithms.AStar.
/// </summary>
namespace CapeGuy.Tests {

	[TestFixture]
	[Category("AStar Tests")]
	internal class AStar_Tests {

		internal class TestGraphElement {
			public Vector2 position { get { return _position; } }

			public static TestGraphElement At (Vector2 position) {
				return new TestGraphElement(position);
			}

			public static float DistanceBetween (TestGraphElement lhs, TestGraphElement rhs) {
				Assert.IsTrue (lhs != null && rhs != null);

				return (lhs.position - rhs.position).magnitude;
			}

			public static bool AreAdjacent (TestGraphElement lhs, TestGraphElement rhs) {
				Vector2 lhsPos = lhs.position;
				Vector2 rhsPos = rhs.position;

				bool adjCol = lhsPos.x == rhsPos.x && Mathf.Abs(lhsPos.y - rhsPos.y) == 1.0f;
				bool adjRow = lhsPos.y == rhsPos.y && Mathf.Abs(lhsPos.x - rhsPos.x) == 1.0f;

				return adjCol || adjRow;
			}

			private TestGraphElement (Vector2 position) {
				_position = position;
			}

			private Vector2 _position;
		}

		[Test]
		public void CompletePathTest () {
			var graph = new Algorithms.AStar<TestGraphElement>.LazyGraph ();

			Func<TestGraphElement, TestGraphElement, float> costFunc = (TestGraphElement lhs, TestGraphElement rhs) => { return TestGraphElement.DistanceBetween (lhs, rhs); };

			IDictionary<Vector2, TestGraphElement> graphElements = new Dictionary<Vector2, TestGraphElement> ();

			Func<Vector2, TestGraphElement> getGraphElementFunc = (Vector2 position) => {
				if (!graphElements.ContainsKey (position)) {
					graphElements[position] = TestGraphElement.At (position);
				}
				Assert.IsTrue (graphElements[position] != null && graphElements[position].position == position);
				return graphElements[position];
			};


			graph.getElementsConnectedToElementFunc = (TestGraphElement testPos) => {
				IList<TestGraphElement> connectedElems = new List<TestGraphElement> ();
				connectedElems.Add (getGraphElementFunc (OffsetPos (testPos.position, Vector2.right)));
				connectedElems.Add (getGraphElementFunc (OffsetPos (testPos.position, -Vector2.right)));
				connectedElems.Add (getGraphElementFunc (OffsetPos (testPos.position, Vector2.up)));
				connectedElems.Add (getGraphElementFunc (OffsetPos (testPos.position, -Vector2.up)));
				return connectedElems;
			};

			graph.getActualCostForMovementBetweenElementsFunc = costFunc;
			graph.getLowestCostEstimateForMovementBetweenElementsFunc = costFunc;

			Assert.IsTrue (graph.GetIsValid ());

			var aStarSearch = new Algorithms.AStar<TestGraphElement>(graph);

			// Calculate a route...
			Vector2 startPos = Vector2.zero;
			Vector2 endPos = new Vector2 (5, 0);
			int xOffset = Mathf.RoundToInt (Mathf.Abs (startPos.x - endPos.x));
			int yOffset = Mathf.RoundToInt (Mathf.Abs (startPos.y - endPos.y));
			int shortestRouteNumElements = xOffset + yOffset + 1;
			IList<TestGraphElement> route = aStarSearch.Calculate (getGraphElementFunc(startPos), getGraphElementFunc(endPos));
			Assert.IsNotNull (route);
			Assert.AreEqual (route.Count, shortestRouteNumElements);
			Assert.AreEqual (route.First ().position, startPos);
			Assert.AreEqual (route.Last ().position, endPos);

			for (int currTestIndex = 1; currTestIndex < route.Count; ++currTestIndex) {
				TestGraphElement prevElem = route[currTestIndex - 1];
				TestGraphElement currElem = route[currTestIndex];
				Assert.IsTrue (TestGraphElement.AreAdjacent (prevElem, currElem));
			}
		}

		[Test]
		public void PartialPathTest () {
			var graph = new Algorithms.AStar<TestGraphElement>.LazyGraph ();

			Func<TestGraphElement, TestGraphElement, float> costFunc = (TestGraphElement lhs, TestGraphElement rhs) => {
				return TestGraphElement.DistanceBetween (lhs, rhs);
			};

			IDictionary<Vector2, TestGraphElement> graphElements = new Dictionary<Vector2, TestGraphElement> ();

			Func<Vector2, TestGraphElement> getGraphElementFunc = (Vector2 position) => {
				if (!graphElements.ContainsKey (position)) {
					graphElements[position] = TestGraphElement.At (position);
				}
				Assert.IsTrue (graphElements[position] != null && graphElements[position].position == position);
				return graphElements[position];
			};

			graph.getElementsConnectedToElementFunc = (TestGraphElement testPos) => {
				// Add a gap in the graph between x=2 and x=3 so we cannot do a complete path.
				float graphBreakXPos = 2.0f;

				IList<TestGraphElement> connectedElems = new List<TestGraphElement> ();
				if (testPos.position.x != graphBreakXPos) {
					connectedElems.Add (getGraphElementFunc (OffsetPos (testPos.position, Vector2.right)));
				}
				if (testPos.position.x != graphBreakXPos + 1.0f) {
					connectedElems.Add (getGraphElementFunc (OffsetPos (testPos.position, -Vector2.right)));
				}
				connectedElems.Add (getGraphElementFunc (OffsetPos (testPos.position, Vector2.up)));
				connectedElems.Add (getGraphElementFunc (OffsetPos (testPos.position, -Vector2.up)));
				return connectedElems;
			};

			graph.getActualCostForMovementBetweenElementsFunc = costFunc;
			graph.getLowestCostEstimateForMovementBetweenElementsFunc = costFunc;

			Assert.IsTrue (graph.GetIsValid ());

			var aStarSearch = new Algorithms.AStar<TestGraphElement>(graph);

			// Calculate a route...
			Vector2 startPos = Vector2.zero;
			Vector2 endPos = new Vector2 (5, 0);

			// Check that we can't find a complete route between the two points (as there is a break in the graph).
			// Note: As we have an non-finite graph and there is no complete path, we MUST give a worst acceptable route cost as otherwise the
			// calculation will never terminate (as it can never be sure there isn't a viable solution in the parts of the graph it hasn't reached yet).
			IList<TestGraphElement> completeRoute = aStarSearch.Calculate (getGraphElementFunc(startPos), getGraphElementFunc(endPos), false, 100.0f);
			Assert.IsNull (completeRoute);

			IList<TestGraphElement> partialRoute = aStarSearch.Calculate (getGraphElementFunc(startPos), getGraphElementFunc(endPos), true, 100.0f);
			Assert.IsNotNull (partialRoute);

			// Check the best partial route we found is what we would expect...
			Assert.AreEqual (partialRoute.Count, 3);
			Assert.AreEqual (partialRoute[0].position, startPos);
			Assert.AreEqual (partialRoute[1].position, new Vector2(1, 0));
			Assert.AreEqual (partialRoute[2].position, new Vector2(2, 0));
		}

		// Test using a struct type as the GraphElemnt type.
		[Test]
		public void TestStructGraphElement () {
			var graph = new Algorithms.AStar<Vector2>.LazyGraph ();

			Func<Vector2, Vector2, float> costFunc = (Vector2 lhs, Vector2 rhs) => {
				return (rhs - lhs).magnitude;
			};

			graph.getActualCostForMovementBetweenElementsFunc = costFunc;
			graph.getLowestCostEstimateForMovementBetweenElementsFunc = costFunc;

			IList<Vector2> connectedOffsets = (new List<Vector2> {Vector2.right, Vector2.left, Vector2.up, Vector2.down}).AsReadOnly ();

			// Connect a grid of all integer positions in both axes up to 100.0f away from the origin.
			graph.getElementsConnectedToElementFunc = (Vector2 startPos) => {
				IList<Vector2> connectedPositions = new List<Vector2> ();

				foreach (Vector2 currConnectedOffset in connectedOffsets) {
					Vector2 currConnectedPos = OffsetPos(startPos, currConnectedOffset);
					if (currConnectedPos.magnitude < 10.0f) {
						connectedPositions.Add(currConnectedPos);
					}
				}

				return connectedPositions;
			};

			Assert.IsTrue (graph.GetIsValid ());

			var aStarSearch = new Algorithms.AStar<Vector2>(graph);

			IList<Vector2> resultPath = aStarSearch.Calculate (new Vector2(-5.0f, 0.0f), new Vector2(5.0f, 0.0f));

			// The expected path is a straight path along the y-axis from x = -5.0f to x = 5.0f.
			IList<Vector2> expectedResultPath = new List<Vector2> ();
			for (int i = -5; i <= 5; ++i) {
				expectedResultPath.Add (new Vector2 ((float)i, 0.0f));
			}

			Assert.IsTrue (resultPath.SequenceEqual(expectedResultPath));
		}

		private Vector2 OffsetPos (Vector2 origPos, Vector2 offset) {
			int xInt = Mathf.RoundToInt (origPos.x);
			int yInt = Mathf.RoundToInt (origPos.y);
			int xOffsetInt = Mathf.RoundToInt (offset.x);
			int yOffsetInt = Mathf.RoundToInt (offset.y);

			int newxInt = xInt + xOffsetInt;
			int newyInt = yInt + yOffsetInt;

			return new Vector2 ((float)newxInt, (float)newyInt);
		}

	}
}