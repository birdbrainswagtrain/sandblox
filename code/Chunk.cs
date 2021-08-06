#nullable enable

using Sandbox;
using System;
using System.Collections.Generic;

namespace Cubism
{
	class GreedyMeshSlice
	{
		private int[,] Data;
		private readonly int Size;

		public GreedyMeshSlice( int size)
		{
			Size = size;
			Data = new int[size,size];
		}

		public void Set(int x, int y, int d)
		{
			Data[y, x] = d;
		}

		private int Get(int x, int y)
		{
			return Data[y, x];
		}

		// callback takes (mat x y w h)
		public IEnumerable<(int,float,float,float,float)> ProcessFaces()
		{
			for (int y=0;y<Size;y++ )
			{
				for (int x=0;x<Size; x++)
				{
					int m = Get( x, y );
					if (m != 0)
					{
						int start_x = x;
						int end_x = x;
						Set( x, y, 0 );
						while (end_x+1 < Size && Get(end_x+1,y) == m)
						{
							end_x++;
							Set( end_x, y, 0 );
						}

						int start_y = y;
						int end_y = y;

						while (end_y +1 < Size)
						{
							int next_y = end_y + 1;
							// Check row.
							for (int ix=start_x;ix<=end_x;ix++)
							{
								if (Get(ix, next_y) != m)
								{
									goto End;
								}
							}
							// Row is good, clear it.
							for ( int ix = start_x; ix <= end_x; ix++ )
							{
								Set( ix, next_y, 0 );
							}
							end_y = next_y;
						}

						End:
						float center_x = (start_x + end_x) / 2f;
						float center_y = (start_y + end_y) / 2f;
						float size_x = end_x - start_x + 1;
						float size_y = end_y - start_y + 1;
						yield return (m, center_x, center_y, size_x, size_y);
					}
				}
			}
		}
	}

	public enum BlockFace : int
	{
		Invalid = -1,
		ZPos = 0,
		ZNeg = 1,
		XNeg = 2,
		YPos = 3,
		XPos = 4,
		YNeg = 5,
	};

	public class Chunk
	{
		public const int ChunkSize = 32;
		private const int MaxFaceCount = 7000;

		public const float BlockScale = 16;

		private readonly Model Model;
		private readonly Mesh Mesh;
		private SceneObject? SceneObject;

		private readonly IntVector3 Offset;

		// Assuming we can remove some bounds checks by using a single-dimensional array.
		private ushort[] Data = new ushort[ChunkSize * ChunkSize * ChunkSize];

		public Chunk( IntVector3 offset )
		{
			this.Offset = offset;

			var material = Material.Load( "materials/default/vertex_color.vmat" );
			Mesh = new Mesh( material );
			Mesh.CreateVertexBuffer<BlockVertex>( MaxFaceCount * 6, BlockVertex.Layout );

			var boundsMin = Vector3.Zero;
			var boundsMax = boundsMin + (new Vector3( ChunkSize ) * BlockScale);
			Mesh.SetBounds( boundsMin, boundsMax );

			Rebuild();

			// BUG: new ModelBuilder(); will result in a broken ModelBuilder
			var mb = Model.Builder;
			mb.AddMesh(Mesh);
			Model = mb.Create();

			var rot = Rotation.Identity;
			var transform = new Transform( new Vector3( offset.x, offset.y, offset.z ) * BlockScale, rot, BlockScale );
			SceneObject = new SceneObject( Model, transform );
		}

		public void Delete()
		{
			if ( SceneObject != null )
			{
				SceneObject.Delete();
				SceneObject = null;
			}
		}

		public ushort GetBlock( int x, int y, int z )
		{
			return Data[x + y * ChunkSize + z * ChunkSize * ChunkSize];
		}

		public void SetBlock( int x, int y, int z, ushort blocktype )
		{
			Data[x + y * ChunkSize + z * ChunkSize * ChunkSize] = blocktype;
		}

		public Chunk? PrevChunkX;
		public Chunk? PrevChunkY;
		public Chunk? PrevChunkZ;

		public void Rebuild( )
		{
			if ( Mesh.IsValid )
			{
				Mesh.LockVertexBuffer<BlockVertex>( Rebuild );
			}
		}

		private void Rebuild( Span<BlockVertex> vertices )
		{
			int vertexOffset = 0;

			var slice = new GreedyMeshSlice( ChunkSize );

			// Z Faces
			for ( int z = 0; z < ChunkSize; z++ )
			{
				for ( int y = 0; y < ChunkSize; y++ )
				{
					for ( int x = 0; x < ChunkSize; x++ )
					{
						ushort blockType = GetBlock( x, y, z );
						ushort blockTypePrev;
						if (z == 0)
						{
							if (PrevChunkZ != null)
							{
								blockTypePrev = PrevChunkZ.GetBlock( x, y, ChunkSize - 1 );
							} else
							{
								blockTypePrev = 0;
							}
						} else
						{
							blockTypePrev = GetBlock( x, y, z - 1 );
						}

						if ( blockType == 0 && blockTypePrev != 0 )
						{
							slice.Set( x, y, blockTypePrev );
						} else if ( blockType != 0 && blockTypePrev == 0 )
						{
							slice.Set( x, y, -blockType );
						}
					}
				}

				foreach ((int mat, float fx, float fy, float w, float h) in slice.ProcessFaces())
				{
					if ( vertexOffset + 6 >= vertices.Length )
						goto End;
					
					if (mat > 0)
					{
						AddQuad( vertices.Slice( vertexOffset, 6 ), fx, fy, z, (int)BlockFace.ZPos, (ushort)mat, w, h );
					} else
					{
						AddQuad( vertices.Slice( vertexOffset, 6 ), fx, fy, z, (int)BlockFace.ZNeg, (ushort)-mat, w, h );
					}
					vertexOffset += 6;
				}
			}

			// Y Faces
			for ( int y = 0; y < ChunkSize; y++ )
			{
				for ( int z = 0; z < ChunkSize; z++ )
				{
					for ( int x = 0; x < ChunkSize; x++ )
					{
						ushort blockType = GetBlock( x, y, z );
						ushort blockTypePrev;
						if ( y == 0 )
						{
							if ( PrevChunkY != null )
							{
								blockTypePrev = PrevChunkY.GetBlock( x, ChunkSize - 1, z );
							}
							else
							{
								blockTypePrev = 0;
							}
						}
						else
						{
							blockTypePrev = GetBlock( x, y - 1, z );
						}

						if ( blockType == 0 && blockTypePrev != 0 )
						{
							slice.Set( x, z, blockTypePrev );
						}
						else if ( blockType != 0 && blockTypePrev == 0 )
						{
							slice.Set( x, z, -blockType );
						}
					}
				}

				foreach ( (int mat, float fx, float fy, float w, float h) in slice.ProcessFaces() )
				{
					if ( vertexOffset + 6 >= vertices.Length )
						goto End;

					if ( mat > 0 )
					{
						AddQuad( vertices.Slice( vertexOffset, 6 ), fx, y, fy, (int)BlockFace.YPos, (ushort)mat, w, h );
					}
					else
					{
						AddQuad( vertices.Slice( vertexOffset, 6 ), fx, y, fy, (int)BlockFace.YNeg, (ushort)-mat, w, h );
					}
					vertexOffset += 6;
				}
			}

			// X Faces
			for ( int x = 0; x < ChunkSize; x++ )
			{
				for ( int z = 0; z < ChunkSize; z++ )
				{
					for ( int y = 0; y < ChunkSize; y++ )
					{
						ushort blockType = GetBlock( x, y, z );
						ushort blockTypePrev;
						if ( x == 0 )
						{
							if ( PrevChunkX != null )
							{
								blockTypePrev = PrevChunkX.GetBlock( ChunkSize - 1, y, z );
							}
							else
							{
								blockTypePrev = 0;
							}
						}
						else
						{
							blockTypePrev = GetBlock( x - 1, y, z );
						}

						if ( blockType == 0 && blockTypePrev != 0 )
						{
							slice.Set( y, z, blockTypePrev );
						}
						else if ( blockType != 0 && blockTypePrev == 0 )
						{
							slice.Set( y, z, -blockType );
						}
					}
				}

				foreach ( (int mat, float fx, float fy, float w, float h) in slice.ProcessFaces() )
				{
					if ( vertexOffset + 6 >= vertices.Length )
						goto End;

					if ( mat > 0 )
					{
						AddQuad( vertices.Slice( vertexOffset, 6 ), x, fx, fy, (int)BlockFace.XPos, (ushort)mat, w, h );
					}
					else
					{
						AddQuad( vertices.Slice( vertexOffset, 6 ), x, fx, fy, (int)BlockFace.XNeg, (ushort)-mat, w, h );
					}
					vertexOffset += 6;
				}
			}

			End:
			Mesh.SetVertexRange( 0, vertexOffset );
		}

		static readonly IntVector3[] BlockVertices = new[]
		{
			new IntVector3( 0, 0, 1 ), // 0
			new IntVector3( 0, 1, 1 ), // 1
			new IntVector3( 1, 1, 1 ), // 2 (unused)
			new IntVector3( 1, 0, 1 ), // 3
			new IntVector3( 0, 0, 0 ), // 4
			new IntVector3( 0, 1, 0 ), // 5
			new IntVector3( 1, 1, 0 ), // 6
			new IntVector3( 1, 0, 0 ), // 7
		};

		static readonly int[] BlockIndices = new[]
		{
			5, 7, 6, 7, 5, 4,
			5, 6, 7, 7, 4, 5,
			5, 4, 0, 0, 1, 5,
			4, 3, 7, 3, 4, 0,
			5, 0, 4, 1, 0, 5,
			4, 7, 3, 3, 0, 4,
		};

		private static void AddQuad( Span<BlockVertex> vertices, float x, float y, float z, int face, ushort blockType, float scaleX = 1, float scaleY = 1 )
		{
			Vector3 scale = Vector3.One;
			Vector3 normal = Vector3.Zero;
			switch ( face )
			{
				case 0: normal = new Vector3( 0, 0, 1 ); scale = new Vector3( scaleX, scaleY, 1 ); break;  // Z+
				case 1: normal = new Vector3( 0, 0, -1 ); scale = new Vector3( scaleX, scaleY, 1 ); break; // Z-

				case 2: normal = new Vector3( -1, 0, 0 ); scale = new Vector3( 1, scaleX, scaleY ); break; // X-
				case 3: normal = new Vector3( 0, 1, 0 ); scale = new Vector3( scaleX, 1, scaleY ); break;  // Y+

				case 4: normal = new Vector3( 1, 0, 0 ); scale = new Vector3( 1, scaleX, scaleY ); break; // X+
				case 5: normal = new Vector3( 0, -1, 0 ); scale = new Vector3( scaleX, 1, scaleY ); break;  // Y-
			}

			var color = Color16.ToColor(blockType);
			//color.r = Rand.Float( );
			//color.g = Rand.Float( );
			//color.b = Rand.Float( );

			for ( int i = 0; i < 6; ++i )
			{
				int vi = BlockIndices[(face * 6) + i];
				var vOffset = BlockVertices[vi];
				vertices[i] = new BlockVertex(
					x + .5f + (vOffset.x - .5f) * scale.x,
					y + .5f + (vOffset.y - .5f) * scale.y,
					z + .5f + (vOffset.z - .5f) * scale.z,
					normal, color);
			}
		}
	}
}
