using Sandbox;

namespace Sandblox
{
	public class FirstPersonCamera : Camera
	{
		public override void Activated()
		{
			var pawn = Local.Pawn;
			if ( pawn == null ) return;

			Pos = pawn.EyePos;
			Rot = pawn.EyeRot;
		}

		public override void Update()
		{
			var pawn = Local.Pawn;
			if ( pawn == null ) return;

			Pos = pawn.EyePos;
			Rot = pawn.EyeRot;

			FieldOfView = 80;

			Viewer = pawn;
		}
	}
}
