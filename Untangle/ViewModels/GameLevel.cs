/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 * 
 * Project:	Untangle
 * 
 * Author:	Aleksandar Dalemski, a_dalemski@yahoo.com
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Untangle.ViewModels
{
    /// <summary>
    /// A view model class for a single game level in a game of Untangle.
    /// </summary>
    public class GameLevel : ViewModelBase
    {
        /// <summary>
        /// Property name constant for the number of intersections remaining in the game level.
        /// </summary>
        public const string IntersectionCountPropertyName = "IntersectionCount";

        /// <summary>
        /// The vertices in the game level.
        /// </summary>
        private readonly Vertex[] _vertices;
        /// <summary>
        /// The line segments in the game level.
        /// </summary>
        private readonly LineSegment[] _lineSegments;
        /// <summary>
        /// A dictionary containing a set of intersecting line segments for each line segment
        /// in the game level.
        /// </summary>
        private readonly Dictionary<LineSegment, HashSet<LineSegment>> _intersections;

        /// <summary>
        /// The vertex which is currently being dragged by the user, if any.
        /// </summary>
        private Vertex _draggedVertex;
        /// <summary>
        /// The vertex which is currently under the mouse cursor, if any.
        /// </summary>
        private Vertex _vertexUnderMouse;
        /// <summary>
        /// The number of intersections remaining in the game level.
        /// </summary>
        private int _intersectionCount;

        /// <summary>
        /// The number of vertices in the game level.
        /// </summary>
        public int VertexCount
        {
            get { return _vertices.Length; }
        }

        /// <summary>
        /// The number of intersections remaining in the game level.
        /// </summary>
        public int IntersectionCount
        {
            get { return _intersectionCount; }
            set
            {
                if (_intersectionCount == value)
                    return;

                _intersectionCount = value;
                OnPropertyChanged(IntersectionCountPropertyName);
            }
        }

        /// <summary>
        /// An enumeration of all vertices and line segments in the game level.
        /// </summary>
        public IEnumerable<object> GameObjects
        {
            get
            {
                var objects = new List<object>(_vertices);
                objects.AddRange(_lineSegments);
                return objects;
            }
        }

        /// <summary>
        /// Specifies whether a vertex is currently being dragged by the user.
        /// </summary>
        public bool IsDragging
        {
            get { return (_draggedVertex != null); }
        }

        /// <summary>
        /// Occurs when the user has successfully solved the level by removing all intersections
        /// between line segments.
        /// </summary>
        public event EventHandler LevelSolved;

        /// <summary>
        /// Initializes a new <see cref="GameLevel"/> instance with the specified lists of vertices
        /// and line segments, and resets the vertices' positions if requested.
        /// </summary>
        /// <param name="vertices">An enumeration of vertices.</param>
        /// <param name="lineSegments">An enumeration of line segments.</param>
        /// <param name="resetPositions">Specifies whether the vertices' positions should be reset,
        /// arranging all vertices in a circle in random order.</param>
        private GameLevel(
            IEnumerable<Vertex> vertices,
            IEnumerable<LineSegment> lineSegments,
            bool resetPositions)
        {
            _vertices = vertices.ToArray();
            _lineSegments = lineSegments.ToArray();
            _intersections = new Dictionary<LineSegment, HashSet<LineSegment>>();
            foreach (LineSegment lineSegment in _lineSegments)
            {
                _intersections[lineSegment] = new HashSet<LineSegment>();
            }
            _draggedVertex = null;
            _intersectionCount = 0;

            if (resetPositions)
            {
                while (_intersectionCount == 0)
                {
                    ResetVertexPositions();
                    CalculateAllIntersections();
                }
            }
            else
                CalculateAllIntersections();
        }

        /// <summary>
        /// Creates a new <see cref="GameLevel"/> instance from an enumeration of vertices loaded
        /// from a saved game file.
        /// </summary>
        /// <param name="savedVertices">An enumeration of vertices loaded from the saved game file.
        /// </param>
        /// <returns>The created game level instance.</returns>
        /// <remarks>
        /// <para>The loaded vertices' positions are preserved during the game level's
        /// initialization.</para>
        /// </remarks>
        public static GameLevel Create(IEnumerable<Saves.Vertex> savedVertices)
        {
            var vertexMappings = new Dictionary<int, Vertex>();
            var lineSegments = new List<LineSegment>();
            foreach (Saves.Vertex savedVertex in savedVertices)
            {
                var vertex = new Vertex();
                vertex.SetPosition(new Point(savedVertex.X, savedVertex.Y));
                vertexMappings[savedVertex.Id] = vertex;
                foreach (int connectedVertexId in savedVertex.ConnectedVertexIds)
                {
                    if (!vertexMappings.ContainsKey(connectedVertexId))
                    {
                        // The connected vertex has not been mapped to a vertex view model yet;
                        // skip this line segment for now, it will be added when the other vertex
                        // is enumerated
                        continue;
                    }

                    Vertex otherVertex = vertexMappings[connectedVertexId];
                    LineSegment lineSegment = vertex.ConnectToVertex(otherVertex);
                    lineSegments.Add(lineSegment);
                }
            }

            return new GameLevel(vertexMappings.Values, lineSegments, false);
        }

        /// <summary>
        /// Creates a new <see cref="GameLevel"/> instance from an enumeration of vertices
        /// generated by a <see cref="Generation.LevelGenerator"/> instance.
        /// </summary>
        /// <param name="generatedVertices">An enumeration of vertices generated by the game level
        /// generator.</param>
        /// <returns>The created game level instance.</returns>
        /// <remarks>
        /// <para>The generated vertices are automatically arranged in a circle in random order
        /// during the game level's initialization.</para>
        /// </remarks>
        public static GameLevel Create(IEnumerable<Generation.Vertex> generatedVertices)
        {
            var vertexMappings = new Dictionary<Generation.Vertex, Vertex>();
            var lineSegments = new List<LineSegment>();
            foreach (Generation.Vertex generatedVertex in generatedVertices)
            {
                var vertex = new Vertex();
                vertexMappings[generatedVertex] = vertex;
                foreach (Generation.Vertex connectedGeneratedVertex in generatedVertex.ConnectedVertices)
                {
                    if (!vertexMappings.ContainsKey(connectedGeneratedVertex))
                    {
                        // The connected vertex has not been mapped to a vertex view model yet;
                        // skip this line segment for now, it will be added when the other vertex
                        // is enumerated
                        continue;
                    }

                    Vertex otherVertex = vertexMappings[connectedGeneratedVertex];
                    LineSegment lineSegment = vertex.ConnectToVertex(otherVertex);
                    lineSegments.Add(lineSegment);
                }
            }

            return new GameLevel(vertexMappings.Values, lineSegments, true);
        }

        /// <summary>
        /// Sets the vertex which is currently under the mouse cursor.
        /// </summary>
        /// <param name="vertex">The vertex which is currently under the mouse cursor.</param>
        public void SetVertexUnderMouse(Vertex vertex)
        {
            if (_vertexUnderMouse == vertex)
                return;

            if (_vertexUnderMouse != null && !IsDragging)
                ChangeVertexState(_vertexUnderMouse, VertexState.Normal);

            _vertexUnderMouse = vertex;
            if (_vertexUnderMouse != null && !IsDragging)
                ChangeVertexState(_vertexUnderMouse, VertexState.UnderMouse);
        }

        /// <summary>
        /// Initiates the dragging of a specified vertex requested by the user.
        /// </summary>
        /// <param name="draggedVertex">The vertex which should be dragged.</param>
        public void StartDrag(Vertex draggedVertex)
        {
            _draggedVertex = draggedVertex;
            _draggedVertex.State = VertexState.Dragged;
        }

        /// <summary>
        /// Moves the vertex which is currently being dragged, to another position on the game
        /// field.
        /// </summary>
        /// <param name="position">The new position of the dragged vertex.</param>
        public void DragVertex(Point position)
        {
            _draggedVertex.SetPosition(position);
        }

        /// <summary>
        /// Completes the dragging of the vertex which is currently being dragged, and
        /// recalculates all current and potential intersections which might have been affected by
        /// the dragging.
        /// </summary>
        public void FinishDrag()
        {
            ChangeVertexState(_draggedVertex, VertexState.Normal);
            RecalculateIntersectionsForVertex(_draggedVertex);
            _draggedVertex = null;

            if (_vertexUnderMouse != null)
                ChangeVertexState(_vertexUnderMouse, VertexState.UnderMouse);

            if (_intersectionCount == 0)
                OnLevelSolved();
        }

        /// <summary>
        /// Resets the positions of all vertices in the game level, arranging them in a circle in
        /// random order.
        /// </summary>
        private void ResetVertexPositions()
        {
            var random = new Random();
            var verticesToScramble = _vertices.ToList();
            int i = 0;
            while (verticesToScramble.Count > 0)
            {
                int vertexIndex = random.Next(verticesToScramble.Count);

                Vertex vertex = verticesToScramble[vertexIndex];
                double angle = Math.PI * 2 * i / _vertices.Length;
                var position = new Point(Math.Cos(angle) * 300.0, -Math.Sin(angle) * 300.0);
                vertex.SetPosition(position);

                verticesToScramble.RemoveAt(vertexIndex);
                i++;
            }
        }

        /// <summary>
        /// Traverses all line segments in the game level and identifies all intersections between
        /// them.
        /// </summary>
        private void CalculateAllIntersections()
        {
            ClearIntersections();
            foreach (LineSegment lineSegment in _lineSegments)
            {
                HashSet<LineSegment> intersectingSegments = _intersections[lineSegment];
                foreach (LineSegment otherSegment in _lineSegments)
                {
                    if (otherSegment == lineSegment || intersectingSegments.Contains(otherSegment))
                        continue;

                    if (CalculationHelper.CheckLinesIntersect(
                        lineSegment.Point1,
                        lineSegment.Point2,
                        otherSegment.Point1,
                        otherSegment.Point2))
                    {
                        AddIntersection(lineSegment, otherSegment);
                    }
                }

                lineSegment.State = (intersectingSegments.Count > 0
                                        ? LineSegmentState.Intersected
                                        : LineSegmentState.Normal);
            }
        }

        /// <summary>
        /// Cleans up the set of intersecting line segments for each line segment in the game level
        /// and resets the number of intersections in the game level to 0.
        /// </summary>
        private void ClearIntersections()
        {
            foreach (LineSegment lineSegment in _lineSegments)
            {
                _intersections[lineSegment].Clear();
                lineSegment.State = LineSegmentState.Normal;
            }
            _intersectionCount = 0;
        }

        /// <summary>
        /// Adds an intersection between two line segments.
        /// </summary>
        /// <param name="lineSegment1">The first line segment.</param>
        /// <param name="lineSegment2">The second line segment.</param>
        /// <remarks>
        /// <para>Each of the two line segments is added to the other's set of intersecting line
        /// segments and the number of intersections in the game level is increased by 1.</para>
        /// </remarks>
        private void AddIntersection(LineSegment lineSegment1, LineSegment lineSegment2)
        {
            _intersections[lineSegment1].Add(lineSegment2);
            _intersections[lineSegment2].Add(lineSegment1);
            IntersectionCount++;

            lineSegment1.State = LineSegmentState.Intersected;
            lineSegment2.State = LineSegmentState.Intersected;
        }

        /// <summary>
        /// Removes an intersection between two line segments.
        /// </summary>
        /// <param name="lineSegment1">The first line segment.</param>
        /// <param name="lineSegment2">The second line segment.</param>
        /// <remarks>
        /// <para>Each of the two line segments is removed from the other's set of intersecting
        /// line segments and the number of intersections in the game level is decreased by 1.
        /// </para>
        /// </remarks>
        private void RemoveIntersection(LineSegment lineSegment1, LineSegment lineSegment2)
        {
            HashSet<LineSegment> intersectingSegments1 = _intersections[lineSegment1];
            HashSet<LineSegment> intersectingSegments2 = _intersections[lineSegment2];

            intersectingSegments1.Remove(lineSegment2);
            intersectingSegments2.Remove(lineSegment1);
            IntersectionCount--;

            lineSegment1.State = (intersectingSegments1.Count > 0
                                    ? LineSegmentState.Intersected
                                    : LineSegmentState.Normal);
            lineSegment2.State = (intersectingSegments2.Count > 0
                                    ? LineSegmentState.Intersected
                                    : LineSegmentState.Normal);

        }

        /// <summary>
        /// Identifies any changes in the intersections between line segments attached to a
        /// specific vertex and any line segments in the game level, after that vertex has been
        /// dragged to a new position.
        /// </summary>
        /// <param name="vertex">The vertex which has been dragged to a new position.</param>
        /// <remarks>
        /// <para>The a single vertex has been dragged to a new position, only the intersections
        /// of the line segments attached to it might have changed, so it is unnecessary to
        /// recalculate all intersections in the game level.</para>
        /// </remarks>
        private void RecalculateIntersectionsForVertex(Vertex vertex)
        {
            foreach (LineSegment lineSegment in vertex.LineSegments)
            {
                HashSet<LineSegment> intersectingSegments = _intersections[lineSegment];
                foreach (LineSegment otherSegment in _lineSegments)
                {
                    if (otherSegment == lineSegment)
                        continue;

                    if (CalculationHelper.CheckLinesIntersect(
                        lineSegment.Point1,
                        lineSegment.Point2,
                        otherSegment.Point1,
                        otherSegment.Point2))
                    {
                        if (!intersectingSegments.Contains(otherSegment))
                            AddIntersection(lineSegment, otherSegment);
                    }
                    else if (intersectingSegments.Contains(otherSegment))
                        RemoveIntersection(lineSegment, otherSegment);
                }
            }
        }

        /// <summary>
        /// Changes the current state of a vertex and possibly the states of vertices which are
        /// directly connected to it and line segments which are attached to it.
        /// </summary>
        /// <param name="vertex">The vertex whose state should be changed.</param>
        /// <param name="state">The new state of the vertex.</param>
        private void ChangeVertexState(Vertex vertex, VertexState state)
        {
            VertexState oldState = vertex.State;
            vertex.State = state;

            if (state == VertexState.Dragged || state == VertexState.UnderMouse)
            {
                if (oldState != VertexState.Dragged && oldState != VertexState.UnderMouse)
                {
                    // The vertex was neither under the mouse, nor was it being dragged by the
                    // user, but now either of those events has occurred
                    foreach (Vertex connectedVertex in vertex.ConnectedVertices)
                    {
                        connectedVertex.State = VertexState.ConnectedToHighlighted;
                    }
                    foreach (LineSegment lineSegment in vertex.LineSegments)
                    {
                        lineSegment.State = LineSegmentState.Highlighted;
                    }
                }
            }
            else if (oldState == VertexState.Dragged || oldState == VertexState.UnderMouse)
            {
                // The vertex was under the mouse or was being dragged by the user, but
                // that is no longer the case
                foreach (Vertex connectedVertex in vertex.ConnectedVertices)
                {
                    connectedVertex.State = VertexState.Normal;
                }
                foreach (LineSegment lineSegment in vertex.LineSegments)
                {
                    lineSegment.State = (_intersections[lineSegment].Count > 0
                                            ? LineSegmentState.Intersected
                                            : LineSegmentState.Normal);
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="LevelSolved"/> event.
        /// </summary>
        private void OnLevelSolved()
        {
            if (LevelSolved != null)
                LevelSolved(this, EventArgs.Empty);
        }
    }
}