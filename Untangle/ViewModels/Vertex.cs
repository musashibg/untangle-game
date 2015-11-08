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
using System.Windows;

namespace Untangle.ViewModels
{
    /// <summary>
    /// A view model class for a single vertex in a game level.
    /// </summary>
    public class Vertex : ViewModelBase
    {
        /// <summary>
        /// Property name constant for the current state of the vertex.
        /// </summary>
        public const string StatePropertyName = "State";
        /// <summary>
        /// Property name constant for the Z index of the vertex.
        /// </summary>
        public const string ZIndexPropertyName = "ZIndex";

        /// <summary>
        /// A dictionary of vertices direcctly connected to the vertex and the line segments
        /// connecting them.
        /// </summary>
        private readonly Dictionary<Vertex, LineSegment> _lineSegmentsMap;

        /// <summary>
        /// The position of the vertex on the game field.
        /// </summary>
        private Point _position;
        /// <summary>
        /// The current state of the vertex.
        /// </summary>
        private VertexState _state;

        /// <summary>
        /// An enumeration of all vertices which are directly connected to the vertex.
        /// </summary>
        public IEnumerable<Vertex> ConnectedVertices
        {
            get { return _lineSegmentsMap.Keys; }
        }

        /// <summary>
        /// An enumeration of all line segments which are attached to the vertex.
        /// </summary>
        public IEnumerable<LineSegment> LineSegments
        {
            get { return _lineSegmentsMap.Values; }
        }

        /// <summary>
        /// The number of line segments which are attached to the vertex.
        /// </summary>
        public int LineSegmentCount
        {
            get { return _lineSegmentsMap.Count; }
        }

        /// <summary>
        /// The size of the vertex on the game field.
        /// </summary>
        public double Size
        {
            get { return 15.0; }
        }

        /// <summary>
        /// The X coordinate of the vertex on the game field.
        /// </summary>
        public double X
        {
            get { return _position.X; }
        }

        /// <summary>
        /// The Y coordinate of the vertex on the game field.
        /// </summary>
        public double Y
        {
            get { return _position.Y; }
        }

        /// <summary>
        /// The current state of the vertex.
        /// </summary>
        public VertexState State
        {
            get { return _state; }
            set
            {
                if (_state == value)
                    return;

                _state = value;
                OnPropertyChanged(StatePropertyName);
                OnPropertyChanged(ZIndexPropertyName);
            }
        }

        /// <summary>
        /// The Z index of the vertex.
        /// </summary>
        /// <remarks>
        /// <para>Z indices of game objects are used to pull/push them to the front/back of the
        /// display surface, possibly obscuring other overlapping objects. Dragged and highlighted
        /// vertices are deliberately pulled to the front of the drawing surface so that they do
        /// not get obscured by other vertices.</para>
        /// </remarks>
        public int ZIndex
        {
            get
            {
                switch (State)
                {
                    case VertexState.ConnectedToHighlighted:
                        return 1;
                    case VertexState.Dragged:
                    case VertexState.UnderMouse:
                        return 2;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Initializes a new <see cref="Vertex"/> instance with no specific position on the game
        /// field.
        /// </summary>
        public Vertex()
            : this(0.0, 0.0)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="Vertex"/> instance with the specified position on the game
        /// field.
        /// </summary>
        /// <param name="x">The X coordinate of the vertex on the game field.</param>
        /// <param name="y">The Y coordinate of the vertex on the game field.</param>
        public Vertex(double x, double y)
        {
            _position = new Point(x, y);
            _state = VertexState.Normal;
            _lineSegmentsMap = new Dictionary<Vertex, LineSegment>();
        }

        /// <summary>
        /// Connects the vertex to another vertex with a line segment and returns it.
        /// </summary>
        /// <param name="otherVertex">The vertex which should be connected to the current one.
        /// </param>
        /// <returns>The created line segment between the two vertices.</returns>
        /// <exception cref="InvalidOperationException">
        /// An attempt is made to connect the vertex to itself.
        /// 
        /// -or-
        /// A line segment between the two vertices already exists.
        /// </exception>
        public LineSegment ConnectToVertex(Vertex otherVertex)
        {
            if (this == otherVertex)
                throw new InvalidOperationException("A vertex cannot be connected to itself.");
            if (_lineSegmentsMap.ContainsKey(otherVertex))
            {
                throw new InvalidOperationException(
                    "A line segment between the two vertices already exists.");
            }

            var lineSegment = new LineSegment(this, otherVertex);
            _lineSegmentsMap[otherVertex] = lineSegment;
            otherVertex._lineSegmentsMap[this] = lineSegment;
            return lineSegment;
        }

        /// <summary>
        /// Changes the position of the vertex on the game field.
        /// </summary>
        /// <param name="position">The new position of the vertex.</param>
        public void SetPosition(Point position)
        {
            _position = position;
            OnPropertyChanged("X");
            OnPropertyChanged("Y");
        }
    }
}