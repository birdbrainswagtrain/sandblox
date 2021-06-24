using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Sandbox;

namespace Sandblox
{
	public class Color16
	{
		public static ushort FromInt(int x)
		{
			float r_float = ((x>>16) & 0xFF) / 255f;
			float g_float = ((x>>8) & 0xFF) / 255f;
			float b_float = (x & 0xFF) / 255f;

			return (ushort)(1
				+ (int)Math.Round( r_float * 39 )
				+ (int)Math.Round( g_float * 39 ) * 40
				+ (int)Math.Round( b_float * 39 ) * 40 * 40);
		}

		public static Color ToColor(ushort n)
		{
			n -= 1;
			float r = (n % 40) / 39f;
			n = (ushort)(n / 40);
			float g = (n % 40) / 39f;
			n = (ushort)(n / 40);
			float b = (n % 40) / 39f;
			return new Color( r, g, b );
		}

		[ServerCmd("test_colors")]
		public static void TestColors()
		{
			Log.Info( "-> " + ToColor( FromInt( 0xa77e4b ) ) );
		}
	}
}
