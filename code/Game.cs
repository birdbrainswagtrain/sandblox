using Sandbox;
using System.Collections.Generic;
using System.Diagnostics;
using System;

namespace Cubism
{
	[Library( "Cubism" )]
	public partial class Game : Sandbox.Game
	{
		private readonly Map Map;

		[ClientCmd("fix_light")]
		public static void FixLightCmd()
		{
			var sun = new SunLight( new Vector3( 1, 0, 0 ), new Color( 1, 0, 0 ) );
			sun.LightColor = Color.White;
			sun.WorldAng = new Angles( -40, 60, 0 );
		}

		public Game()
		{
			if ( IsServer )
			{
				_ = new HudEntity();
			} else
			{
				var sw = Stopwatch.StartNew();
				var loaded = Import.VBSP.Load( "files/rp_downtown_v2.bsp", Chunk.BlockScale );

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
				Log.Warning( $"Time: {sw.Elapsed.TotalMilliseconds}" );

				Map = new Map();

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
									Map.SetBlock( offset_x + x, offset_y + y, offset_z + z, d );
								}
							}
						}
					}
				}

				Map.RebuildAllChunks();
			}
		}

		/*protected override void OnDestroy()
		{
			base.OnDestroy();

			if ( chunks != null )
			{
				foreach ( var chunk in chunks )
				{
					chunk.Delete();
				}
			}
		}*/

		public override void ClientJoined( Client client )
		{
			base.ClientJoined( client );

			var player = new Player();
			client.Pawn = player;

			player.Respawn();
		}
	}
}
