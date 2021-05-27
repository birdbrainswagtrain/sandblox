using System;

namespace Sandblox
{
	public struct IntVector3
	{
		public int x;
		public int y;
		public int z;

		public IntVector3( int x, int y, int z )
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public int Get( int index )
		{
			return index switch
			{
				0 => x,
				1 => y,
				2 => z,
				_ => throw new IndexOutOfRangeException(),
			};
		}
	}
}
