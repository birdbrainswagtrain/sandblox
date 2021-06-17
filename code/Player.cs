using Sandbox;

namespace Sandblox
{
	partial class Player : Sandbox.Player
	{
		public override void Respawn()
		{
			SetModel( "models/citizen/citizen.vmdl" );

			Controller = new NoclipController();
			Camera = new ThirdPersonCamera();

			EnableAllCollisions = true;
			EnableDrawing = true;
			EnableHideInFirstPerson = true;
			EnableShadowInFirstPerson = true;

			base.Respawn();
		}

		public override void Simulate( Client cl )
		{
			base.Simulate( cl );

			if ( IsClient && (Input.Down( InputButton.Duck ) ? Input.Down( InputButton.Attack1 ) : Input.Pressed( InputButton.Attack1 )) )
			{
				(Sandbox.Game.Current as Game).SetBlock( EyePos, EyeRot.Forward, (byte)Rand.Int( 1, 5 ) );
			}
			else if ( IsClient && (Input.Down( InputButton.Duck ) ? Input.Down( InputButton.Attack2 ) : Input.Pressed( InputButton.Attack2 )) )
			{
				(Sandbox.Game.Current as Game).SetBlock( EyePos, EyeRot.Forward, 0 );
			}
		}
	}
}
