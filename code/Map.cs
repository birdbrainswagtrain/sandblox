using Sandbox;
using System;
using System.Collections.Generic;

namespace Cubism
{
	public class Map
	{
		private Dictionary<IntVector3, Chunk> Chunks = new Dictionary<IntVector3, Chunk>();
		//private readonly static int[] xOffsets = new[] { 0, 0, -1, 0, 1, 0 };
		//private readonly static int[] yOffsets = new[] { 0, 0, 0, 1, 0, -1 };
		//private readonly static int[] zOffsets = new[] { 1, -1, 0, 0, 0, 0 };

		public Map()
		{

		}

		public ushort GetBlock( int x, int y, int z )
		{
			// I am not sure C# is smart enough to optimize these, especially in debug mode.
			int chunk_x = x / Chunk.ChunkSize;
			int chunk_y = y / Chunk.ChunkSize;
			int chunk_z = z / Chunk.ChunkSize;

			int local_x = x % Chunk.ChunkSize;
			int local_y = y % Chunk.ChunkSize;
			int local_z = z % Chunk.ChunkSize;

			var chunk_pos = new IntVector3( chunk_x, chunk_y, chunk_z );
			var chunk = GetOrInitChunk( chunk_pos );
			return chunk.GetBlock( local_x, local_y, local_z );
		}

		public void SetBlock( int x, int y, int z, ushort blocktype)
		{
			// I am not sure C# is smart enough to optimize these, especially in debug mode.
			int chunk_x = x / Chunk.ChunkSize;
			int chunk_y = y / Chunk.ChunkSize;
			int chunk_z = z / Chunk.ChunkSize;

			int local_x = x % Chunk.ChunkSize;
			int local_y = y % Chunk.ChunkSize;
			int local_z = z % Chunk.ChunkSize;

			var chunk_pos = new IntVector3( chunk_x, chunk_y, chunk_z );
			var chunk = GetOrInitChunk( chunk_pos );
			chunk.SetBlock( local_x, local_y, local_z, blocktype );
		}

		private Chunk GetOrInitChunk( in IntVector3 chunk_pos )
		{
			var chunk = Chunks.GetValueOrDefault( chunk_pos );

			if ( chunk == null )
			{
				chunk = new Chunk( chunk_pos * Chunk.ChunkSize );
				Chunks[chunk_pos] = chunk;
			}

			return chunk;
		}

		private Chunk TryGetChunk( in IntVector3 chunk_pos  )
		{
			return Chunks.GetValueOrDefault( chunk_pos );
		}

		public void RebuildAllChunks()
		{
			foreach (var pair in Chunks)
			{
				var pos = pair.Key;
				var chunk = pair.Value;
				chunk.PrevChunkX = TryGetChunk( new IntVector3( pos.x-1, pos.y, pos.z ) );
				chunk.PrevChunkY = TryGetChunk( new IntVector3( pos.x, pos.y-1, pos.z ) );
				chunk.PrevChunkZ = TryGetChunk( new IntVector3( pos.x, pos.y, pos.z-1 ) );

				chunk.Rebuild();
			}
		}

		//private static readonly int[] FaceAxis = new[] { 1, 1, 2, 2, 0, 0 };

		/*
		public BlockFace GetBlockInDirection( Vector3 position, Vector3 direction, float length, out IntVector3 hitPosition, out float distance )
		{
			hitPosition = new IntVector3( 0, 0, 0 );
			distance = 0;

			if ( direction.Length <= 0.0f )
			{
				return BlockFace.Invalid;
			}

			// distance from block position to edge of block
			IntVector3 edgeOffset = new( direction.x < 0 ? 0 : 1,
								direction.y < 0 ? 0 : 1,
								direction.z < 0 ? 0 : 1 );

			// amount to step in each direction
			IntVector3 stepAmount = new( direction.x < 0 ? -1 : 1,
								direction.y < 0 ? -1 : 1,
								direction.z < 0 ? -1 : 1 );

			// face that will be hit in each direction
			IntVector3 faceDirection = new( direction.x < 0 ? (int)BlockFace.North : (int)BlockFace.South,
								   direction.y < 0 ? (int)BlockFace.East : (int)BlockFace.West,
								   direction.z < 0 ? (int)BlockFace.Top : (int)BlockFace.Bottom );

			Vector3 position3f = position; // start position
			distance = 0; // distance from starting position
			Ray ray = new( position, direction );

			//// first check against an aabb when tracing outside the bounds
			//if ( position.x < 0.0f || position.y < 0.0f || position.z < 0.0f ||
			//	 position.x >= SizeX || position.y >= SizeY || position.y >= SizeZ )
			//{
			//	BBox aabb = new( new Vector3( 0.0f, 0.0f, 0.0f ), new Vector3( SizeX, SizeY, SizeZ ) );

			//	var sideHit = BlockFace.Invalid;
			//	float d = 0;

			//	if ( sideHit != BlockFace.Invalid )
			//	{
			//		// hit a side but it happend after the length cap, exit
			//		if ( d > length )
			//		{
			//			// made it all the way there
			//			distance = length;

			//			return BlockFace.Invalid;
			//		}

			//		distance = d;
			//		position3f = position + direction * distance; // start position

			//		Vector3 hitPosition3f = position + direction * d;
			//		hitPosition3f[FaceAxis[(int)sideHit]] = MathF.Floor( hitPosition3f[FaceAxis[(int)sideHit]] + 0.5f * stepAmount.Get( FaceAxis[(int)sideHit] ) );

			//		IntVector3 blockHitPosition = new( (int)hitPosition3f.x, (int)hitPosition3f.y, (int)hitPosition3f.z );

			//		var blockType = GetBlockData( blockHitPosition.x, blockHitPosition.y, blockHitPosition.z );

			//		if ( blockType != 0 )
			//		{
			//			hitPosition = blockHitPosition;

			//			return sideHit;
			//		}
			//	}
			//}

			while ( true )
			{
				IntVector3 position3i = new( (int)position3f.x, (int)position3f.y, (int)position3f.z ); // position of the block we are in

				// distance from current position to edge of block we are in
				Vector3 distanceToNearestEdge = new( position3i.x - position3f.x + edgeOffset.x,
												   position3i.y - position3f.y + edgeOffset.y,
												   position3i.z - position3f.z + edgeOffset.z );

				// if we are touching an edge, we are 1 unit away from the next edge
				for ( int i = 0; i < 3; ++i )
				{
					if ( MathF.Abs( distanceToNearestEdge[i] ) == 0.0f )
					{
						distanceToNearestEdge[i] = stepAmount.Get( i );
					}
				}

				// length we must travel along the vector to reach the nearest edge in each direction
				Vector3 lengthToNearestEdge = new( MathF.Abs( distanceToNearestEdge.x / direction.x ),
												 MathF.Abs( distanceToNearestEdge.y / direction.y ),
												 MathF.Abs( distanceToNearestEdge.z / direction.z ) );

				int axis;

				// if the nearest edge in the x direction is the closest
				if ( lengthToNearestEdge.x < lengthToNearestEdge.y && lengthToNearestEdge.x < lengthToNearestEdge.z )
				{
					axis = 0;
				}
				// if the nearest edge in the y direction is the closest
				else if ( lengthToNearestEdge.y < lengthToNearestEdge.x && lengthToNearestEdge.y < lengthToNearestEdge.z )
				{
					axis = 1;
				}
				// if nearest edge in the z direction is the closest
				else
				{
					axis = 2;
				}

				distance += lengthToNearestEdge[axis];
				position3f = position + direction * distance;
				position3f[axis] = MathF.Floor( position3f[axis] + 0.5f * stepAmount.Get( axis ) );

				if ( position3f.x < 0.0f || position3f.y < 0.0f || position3f.z < 0.0f ||
					 position3f.x >= SizeX || position3f.y >= SizeY || position3f.z >= SizeZ )
				{
					break;
				}

				// last face hit
				BlockFace lastFace = (BlockFace)faceDirection.Get( axis );

				// if we reached the length cap, exit
				if ( distance > length )
				{
					// made it all the way there
					distance = length;

					return BlockFace.Invalid;
				}

				// if there is a block at the current position, we have an intersection
				position3i = new( (int)position3f.x, (int)position3f.y, (int)position3f.z );

				ushort blockType = GetBlockData( position3i.x, position3i.y, position3i.z );

				if ( blockType != 0 )
				{
					hitPosition = position3i;

					return lastFace;
				}
			}

			Plane plane = new( new Vector3( 0.0f, 0.0f, 0.0f ), new Vector3( 0.0f, 1.0f, 0.0f ) );
			float distanceHit = 0;
			var traceHitPos = plane.Trace( ray, true );
			if ( traceHitPos.HasValue ) distanceHit = Vector3.DistanceBetween( position, traceHitPos.Value );

			if ( distanceHit >= 0.0f && distanceHit <= length )
			{
				Vector3 hitPosition3f = position + direction * distanceHit;

				if ( hitPosition3f.x < 0.0f || hitPosition3f.y < 0.0f || hitPosition3f.z < 0.0f ||
					 hitPosition3f.x > SizeX || hitPosition3f.y > SizeY || hitPosition3f.z > SizeZ )
				{
					// made it all the way there
					distance = length;

					return BlockFace.Invalid;
				}

				hitPosition3f.z = 0.0f;
				IntVector3 blockHitPosition = new( (int)hitPosition3f.x, (int)hitPosition3f.y, (int)hitPosition3f.z );

				ushort blockType = GetBlockData( blockHitPosition.x, blockHitPosition.y, blockHitPosition.z );

				if ( blockType == 0 )
				{
					distance = distanceHit;
					hitPosition = blockHitPosition;
					hitPosition.y = -1;

					return BlockFace.Top;
				}
			}

			// made it all the way there
			distance = length;

			return BlockFace.Invalid;
		}*/
	}
}
