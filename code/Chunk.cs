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

	public class Chunk
	{
		public static readonly int ChunkSize = 32;
		private static readonly int MaxFaceCount = 7000;

		public const float BlockScale = 16;

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
				// TODO just reset on each slice
				// TODO fold both directions into a single slice
				var slice_top = new GreedyMeshSlice(ChunkSize);
				var slice_bot = new GreedyMeshSlice( ChunkSize );
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
									slice_top.Set( x, y, blockType );
									continue;
								}

								if ( face == 1 )
								{
									slice_bot.Set( x, y, blockType );
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

				foreach ((int mat, float x, float y, float w, float h) in slice_top.ProcessFaces())
				{
					if ( vertexOffset + 6 >= vertices.Length )
						goto End;

					AddQuad( vertices.Slice( vertexOffset, 6 ), x, y, z, 0, (ushort)mat, w, h );
					vertexOffset += 6;
				}

				foreach ( (int mat, float x, float y, float w, float h) in slice_bot.ProcessFaces() )
				{
					if ( vertexOffset + 6 >= vertices.Length )
						goto End;

					AddQuad( vertices.Slice( vertexOffset, 6 ), x, y, z, 1, (ushort)mat, w, h );
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
				case 1: normal = new Vector3( 0, 0, -1 ); scale = new Vector3( scaleX, scaleY, 1 ); break; // Z-

				case 2: normal = new Vector3( -1, 0, 0 ); break; // X-
				case 3: normal = new Vector3( 0, 1, 0 ); break;  // Y+

				case 4: normal = new Vector3( 1, 0, 0 ); break; // X+
				case 5: normal = new Vector3( 0, -1, 0 ); break;  // Y-
			}

			var color = Color.White;
			switch (blockType)
			{
				case 1:  color = Color.Parse( "#aaaaaa" ).Value; break; // CONCRETE
				case 2:  color = Color.Parse( "#82372b" ).Value; break; // BRICK
				case 4:  color = Color.Parse( "#ffff00" ).Value; break; // BUILDING TEMPLATE?
				case 8:  color = Color.Parse( "#eddac0" ).Value; break; // PLASTER
				case 10: color = Color.Parse( "#59a12d" ).Value; break; // GRASS
				case 20: color = Color.Parse( "#ff00dd" ).Value; break; // UNKNOWN

				default: Log.Info( "?? " + blockType ); break;
			}
			//color.r += Rand.Float( -.1f, .1f );
			//color.g += Rand.Float( -.1f, .1f );
			//color.b += Rand.Float( -.1f, .1f );

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
