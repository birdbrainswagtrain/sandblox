using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cubism.Import
{
	class ImportWorld
	{
		public Dictionary<(int, int, int), Chunk> Chunks = new Dictionary<(int, int, int), Chunk>();

		public float Scale = 40;

		public void Set( int x, int y, int z, ushort d )
		{
			if ( x < 0 || y < 0 || z < 0 )
				throw new Exception( "Bad coordinate." );

			// Get chunk
			var chunk_coords = (x >> 5, y >> 5, z >> 5);
			Chunk chunk;
			if ( !Chunks.TryGetValue( chunk_coords, out chunk ) )
			{
				chunk = new Chunk();
				Chunks.Add( chunk_coords, chunk );
			}

			chunk.Set( x & 0x1F, y & 0x1F, z & 0x1F, d );

			// Force chunks above blocks to init
			/*if ( (z & 0x1F) == 31 )
			{
				chunk_coords.Item3++;
				if ( !chunks.TryGetValue( chunk_coords, out chunk ) )
				{
					chunks.Add( chunk_coords, new Chunk() );
				}
			}*/
		}
	}

	class Chunk
	{
		ushort[,,] data = new ushort[32, 32, 32];

		public void Set( int x, int y, int z, ushort d )
		{
			data[z, y, x] = d;
		}

		public ushort Get( int x, int y, int z )
		{
			return data[z, y, x];
		}
	}
}
