using Sandbox;
using System.Collections.Generic;
using System.Diagnostics;
using System;

namespace Sandblox
{
	[Library( "sandblox" )]
	public partial class Game : Sandbox.Game
	{
		private readonly Map map;
		private readonly Chunk[] chunks;

		public Game()
		{
			if ( IsServer )
			{
				_ = new HudEntity();
			}

			var sw = Stopwatch.StartNew();
			var loaded = Import.VBSP.Load( "gm_construct_9.bsp", Chunk.BlockScale );

			int max_x = Int32.MinValue;
			int max_y = Int32.MinValue;
			int max_z = Int32.MinValue;

			int min_x = Int32.MaxValue;
			int min_y = Int32.MaxValue;
			int min_z = Int32.MaxValue;

			foreach (var pair in loaded.Chunks)
			{
				var pos = pair.Key;

				max_x = Math.Max( max_x, (pos.Item1+1) * 32 );
				max_y = Math.Max( max_y, (pos.Item2+1) * 32 );
				max_z = Math.Max( max_z, (pos.Item3+1) * 32 );

				min_x = Math.Min( min_x, pos.Item1 * 32 );
				min_y = Math.Min( min_y, pos.Item2 * 32 );
				min_z = Math.Min( min_z, pos.Item3 * 32 );
			}
			Log.Warning( $". {min_x} {min_y} {min_z}" );
			Log.Warning( $"--> {sw.Elapsed.TotalMilliseconds}" );

			map = new Map( max_x - min_x, max_y - min_y, max_z - min_z );

			foreach ( var pair in loaded.Chunks )
			{
				var pos = pair.Key;
				int offset_x = pos.Item1 * 32 - min_x;
				int offset_y = pos.Item2 * 32 - min_y;
				int offset_z = pos.Item3 * 32 - min_z;

				var chunk = pair.Value;

				for ( int z=0;z<32;z++)
				{
					for (int y=0;y<32;y++)
					{
						for (int x=0;x<32;x++)
						{
							var d = chunk.Get( x, y, z );
							if (d != 0)
							{
								map.SetBlock( offset_x + x, offset_y + y, offset_z + z, d );
							}
						}
					}
				}
			}

			if ( IsClient )
			{
				var numChunksX = map.SizeX / Chunk.ChunkSize;
				var numChunksY = map.SizeY / Chunk.ChunkSize;
				var numChunksZ = map.SizeZ / Chunk.ChunkSize;

				chunks = new Chunk[(numChunksX * numChunksY * numChunksZ)];

				for ( int x = 0; x < numChunksX; ++x )
				{
					for ( int y = 0; y < numChunksY; ++y )
					{
						for ( int z = 0; z < numChunksZ; ++z )
						{
							var chunkIndex = x + y * numChunksX + z * numChunksX * numChunksY;
							var chunk = new Chunk( map, new IntVector3( x * Chunk.ChunkSize, y * Chunk.ChunkSize, z * Chunk.ChunkSize ) );
							chunks[chunkIndex] = chunk;
						}
					}
				}
			}
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			if ( chunks != null )
			{
				foreach ( var chunk in chunks )
				{
					chunk.Delete();
				}
			}
		}

		public override void ClientJoined( Client client )
		{
			base.ClientJoined( client );

			var player = new Player();
			client.Pawn = player;

			player.Respawn();
		}

		public bool SetBlock( Vector3 pos, Vector3 dir, byte blocktype )
		{
			var f = map.GetBlockInDirection( pos * (1.0f / 32.0f), dir.Normal, 10000, out var hitpos, out _ );
			if ( f == Map.BlockFace.Invalid )
				return false;

			var x = hitpos.x;
			var y = hitpos.y;
			var z = hitpos.z;

			if ( blocktype != 0 )
			{
				var b = Map.GetAdjacentPos( x, y, z, (int)f );
				x = b.x;
				y = b.y;
				z = b.z;
			}

			bool build = false;
			var chunkids = new HashSet<int>();

			var numChunksX = map.SizeX / Chunk.ChunkSize;
			var numChunksY = map.SizeY / Chunk.ChunkSize;

			for ( int x2 = 0; x2 < 1; ++x2 )
			{
				for ( int y2 = 0; y2 < 1; ++y2 )
				{
					for ( int z2 = 0; z2 < 1; ++z2 )
					{
						var x3 = x + x2;
						var y3 = y + y2;
						var z3 = z + z2;

						if ( map.SetBlock( x3, y3, z3, blocktype ) )
						{
							var chunkIndex = (x3 / Chunk.ChunkSize) + (y3 / Chunk.ChunkSize) * numChunksX + (z3 / Chunk.ChunkSize) * numChunksX * numChunksY;

							chunkids.Add( chunkIndex );

							build = true;
						}
					}
				}
			}

			foreach ( var chunkid in chunkids )
			{
				chunks[chunkid].Rebuild();
			}

			return build;
		}
	}
}
