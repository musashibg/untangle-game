/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 * 
 * Project:	Untangle
 * 
 * Author:	Aleksandar Dalemski, a_dalemski@yahoo.com
 */

using System.Xml.Serialization;

namespace Untangle.Saves
{
    /// <summary>
    /// Stores information about a single vertex in a saved game level.
    /// </summary>
    public class Vertex
    {
        /// <summary>
        /// The unique identifier of the vertex in the saved game level.
        /// </summary>
        [XmlAttribute]
        public int Id { get; set; }

        /// <summary>
        /// The X coordinate of the vertex on the screen.
        /// </summary>
        [XmlAttribute]
        public double X { get; set; }

        /// <summary>
        /// The Y coordinate of the vertex on the screen.
        /// </summary>
        [XmlAttribute]
        public double Y { get; set; }

        /// <summary>
        /// An array containing the unique identifiers of all vertices which are directly connected
        /// to the current vertex.
        /// </summary>
        [XmlElement("ConnectedVertexId")]
        public int[] ConnectedVertexIds { get; set; }
    }
}