using Sandbox;

namespace Sandblox
{
	[Library]
	public class NoclipController : BasePlayerController
	{
		public override void Simulate()
		{
			var vel = (Input.Rotation.Forward * Input.Forward) + (Input.Rotation.Left * Input.Left);
			vel = vel.Normal * 2000;

			if ( Input.Down( InputButton.Run ) )
				vel *= 5.0f;

			if ( Input.Down( InputButton.Duck ) )
				vel *= 0.2f;

			var pos = Position + vel * Time.Delta;

			Position = pos;
			EyeRot = Input.Rotation;
		}
	}
}
