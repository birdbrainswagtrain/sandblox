using Sandbox;
using System.Runtime.InteropServices;

namespace Sandblox
{
    [StructLayout( LayoutKind.Sequential )]
    public struct BlockVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Color Color;

        public BlockVertex( uint x, uint y, uint z, Vector3 normal, byte blockType )
        {
            Position = new Vector3( x, y, z );
			Normal = normal;
            Color = Color.White;
        }

        public static readonly VertexAttribute[] Layout =
        {
            new VertexAttribute(VertexAttributeType.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttribute(VertexAttributeType.Normal, VertexAttributeFormat.Float32, 3),
            new VertexAttribute(VertexAttributeType.Color, VertexAttributeFormat.Float32, 4)
        };
    }
}
