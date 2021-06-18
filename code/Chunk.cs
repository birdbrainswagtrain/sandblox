﻿using Sandbox;
using System;
using System.Collections.Generic;

namespace Sandblox
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
				int start_x = 0;
				int current_mat = 0;
				for (int x=0;x<Size; x++)
				{
					int m = Get( x, y );
					if (current_mat != m)
					{
						if (current_mat != 0)
						{
							float center_x = (start_x + x - 1) / 2f;
							float center_y = y;
							float size_x = x - start_x;
							float size_y = 1;
							yield return (current_mat, center_x, center_y, size_x, size_y );
						}
						current_mat = m;
						start_x = x;
					}
				}

				if ( current_mat != 0 )
				{
					float center_x = (start_x + Size - 1) / 2f;
					float center_y = y;
					float size_x = Size - start_x;
					float size_y = 1;
					yield return (current_mat, center_x, center_y, size_x, size_y);
				}
			}
		}
	}

	public class Chunk
	{
		public static readonly int ChunkSize = 32;
		private static readonly int MaxFaceCount = 7000;

		public const float BlockScale = 32;

		private readonly Map map;
		private readonly Model model;
		private readonly Mesh mesh;
		private readonly IntVector3 offset;
		private SceneObject sceneObject;

		public Chunk( Map map, IntVector3 offset )
		{
			this.map = map;
			this.offset = offset;

			var material = Material.Load( "materials/default/vertex_color.vmat" );
			mesh = Mesh.Create( material );
			mesh.CreateVertexBuffer<BlockVertex>( MaxFaceCount * 6, BlockVertex.Layout );

			var boundsMin = Vector3.Zero;
			var boundsMax = boundsMin + (new Vector3( ChunkSize ) * BlockScale);
			mesh.SetBounds( boundsMin, boundsMax );

			Rebuild();

			// BUG: new ModelBuilder(); will result in a broken ModelBuilder
			var mb = Model.Builder;
			mb.AddMesh(mesh);
			model = mb.Create();

			var rot = Rotation.Identity;
			var transform = new Transform( new Vector3( offset.x, offset.y, offset.z ) * BlockScale, rot, BlockScale );
			sceneObject = new SceneObject( model, transform );
		}

		public void Delete()
		{
			if ( mesh.IsValid )
			{
				mesh.DeleteBuffers();
			}
			if ( sceneObject != null )
			{
				sceneObject.Delete();
				sceneObject = null;
			}
		}

		public void Rebuild()
		{
			if ( mesh.IsValid )
			{
				mesh.LockVertexBuffer<BlockVertex>( Rebuild );
			}
		}

		private void Rebuild( Span<BlockVertex> vertices )
		{
			int vertexOffset = 0;

			for ( int z = 0; z < ChunkSize; z++ )
			{
				var slice = new GreedyMeshSlice(ChunkSize); // TODO just reset on each slice
				for ( int y = 0; y < ChunkSize; y++ )
				{
					for ( int x = 0; x < ChunkSize; x++ )
					{
						var mx = offset.x + x;
						var my = offset.y + y;
						var mz = offset.z + z;

						var blockIndex = map.GetBlockIndex( mx, my, mz );
						var blockType = map.GetBlockData( blockIndex );

						if ( blockType != 0 )
						{
							for ( int face = 0; face < 6; ++face )
							{
								if ( !map.IsAdjacentBlockEmpty( mx, my, mz, face ) )
									continue;

								if ( face == 0 )
								{
									slice.Set( x, y, blockType );
									continue;
								}

								if ( vertexOffset + 6 >= vertices.Length )
									goto End;

								AddQuad( vertices.Slice( vertexOffset, 6 ), x, y, z, face, blockType );
								vertexOffset += 6;
							}
						}
					}
				}

				// go through slice
				foreach ((int mat, float x, float y, float w, float h) in slice.ProcessFaces())
				{
					if ( vertexOffset + 6 >= vertices.Length )
						goto End;

					AddQuad( vertices.Slice( vertexOffset, 6 ), x, y, z, 0, (ushort)mat, w, h );
					vertexOffset += 6;
				}
			}


			End:
			mesh.SetVertexRange( 0, vertexOffset );
		}

		static readonly IntVector3[] BlockVertices = new[]
		{
			new IntVector3( 0, 0, 1 ),
			new IntVector3( 0, 1, 1 ),
			new IntVector3( 1, 1, 1 ),
			new IntVector3( 1, 0, 1 ),
			new IntVector3( 0, 0, 0 ),
			new IntVector3( 0, 1, 0 ),
			new IntVector3( 1, 1, 0 ),
			new IntVector3( 1, 0, 0 ),
		};

		static readonly int[] BlockIndices = new[]
		{
			2, 1, 0, 0, 3, 2,
			5, 6, 7, 7, 4, 5,
			5, 4, 0, 0, 1, 5,
			6, 5, 1, 1, 2, 6,
			7, 6, 2, 2, 3, 7,
			4, 7, 3, 3, 0, 4,
		};

		private static void AddQuad( Span<BlockVertex> vertices, float x, float y, float z, int face, ushort blockType, float scaleX = 1, float scaleY = 1 )
		{
			Vector3 scale = Vector3.One;
			Vector3 normal = Vector3.Zero;
			switch ( face )
			{
				case 0: normal = new Vector3( 0, 0, 1 ); scale = new Vector3( scaleX, scaleY, 1 ); break;  // Z+
				case 1: normal = new Vector3( 0, 0, -1 ); break; // Z-

				case 2: normal = new Vector3( -1, 0, 0 ); break; // X-
				case 3: normal = new Vector3( 0, 1, 0 ); break;  // Y+

				case 4: normal = new Vector3( 1, 0, 0 ); break; // X+
				case 5: normal = new Vector3( 0, -1, 0 ); break;  // Y-
			}

			var color = new Color();
			color.r = Rand.Float();
			color.g = Rand.Float();
			color.b = Rand.Float();

			for ( int i = 0; i < 6; ++i )
			{
				int vi = BlockIndices[(face * 6) + i];
				var vOffset = BlockVertices[vi];
				vertices[i] = new BlockVertex(
					x + .5f + ((float)vOffset.x - .5f) * scale.x,
					y + vOffset.y * scale.y,
					z + vOffset.z * scale.z,
					normal, color);
			}
		}
	}
}
