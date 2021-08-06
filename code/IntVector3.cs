using System;

namespace Cubism
{
	public struct IntVector3 : IEquatable<IntVector3>
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

		public static IntVector3 operator *( IntVector3 a, int b )
			=> new IntVector3( a.x * b, a.y * b, a.z * b );

		public override int GetHashCode()
		{
			return HashCode.Combine(x.GetHashCode(),y.GetHashCode(),z.GetHashCode());
		}

		public override bool Equals( object obj )
		{
			if (obj is IntVector3)
			{
				return Equals((IntVector3)obj);
			}
			return false;
		}

		public bool Equals( IntVector3 other )
		{
			return x == other.x && y == other.y && z == other.z;
		}
	}
}
