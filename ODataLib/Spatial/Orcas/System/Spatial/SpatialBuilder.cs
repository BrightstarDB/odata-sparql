//   Copyright 2011 Microsoft Corporation
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

namespace System.Spatial
{
    using Diagnostics.CodeAnalysis;

    /// <summary>
    ///   Creates Geometry or Geography instances from spatial data pipelines.
    /// </summary>
    public class SpatialBuilder : SpatialPipeline, IShapeProvider
    {
        /// <summary>
        ///   The builder to be delegated to when this class is accessed from the IGeographyPipeline or IGeographyProvider interfaces.
        /// </summary>
        private readonly IGeographyProvider geographyOutput;

        /// <summary>
        ///   The builder to be delegated to when this class is accessed from the IGeometryPipeline or IGeometryProvider interfaces.
        /// </summary>
        private readonly IGeometryProvider geometryOutput;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpatialBuilder"/> class.
        /// </summary>
        /// <param name="geographyInput">The geography input.</param>
        /// <param name="geometryInput">The geometry input.</param>
        /// <param name="geographyOutput">The geography output.</param>
        /// <param name="geometryOutput">The geometry output.</param>
        public SpatialBuilder(GeographyPipeline geographyInput, GeometryPipeline geometryInput, IGeographyProvider geographyOutput, IGeometryProvider geometryOutput)
            : base(geographyInput, geometryInput)
        {
            this.geographyOutput = geographyOutput;
            this.geometryOutput = geometryOutput;
        }

        /// <summary>
        ///   Fires when the provider constructs a geography object.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Not following the event-handler pattern")]
        public event Action<Geography> ProduceGeography
        {
            add { this.geographyOutput.ProduceGeography += value; }
            remove { this.geographyOutput.ProduceGeography -= value; }
        }

        /// <summary>
        ///   Fires when the provider constructs a geometry object.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Not following the event-handler pattern")]
        public event Action<Geometry> ProduceGeometry
        {
            add { this.geometryOutput.ProduceGeometry += value; }
            remove { this.geometryOutput.ProduceGeometry -= value; }
        }

        /// <summary>
        ///   Gets the geography object that was constructed most recently.
        /// </summary>
        public Geography ConstructedGeography
        {
            get { return this.geographyOutput.ConstructedGeography; }
        }

        /// <summary>
        ///   Gets the geometry object that was constructed most recently.
        /// </summary>
        public Geometry ConstructedGeometry
        {
            get { return this.geometryOutput.ConstructedGeometry; }
        }

        /// <summary>
        ///   Creates an implementation of the builder
        /// </summary>
        /// <returns>Returns the created SpatialBuilder implementation</returns>
        public static SpatialBuilder Create()
        {
            return SpatialImplementation.CurrentImplementation.CreateBuilder();
        }
    }
}
