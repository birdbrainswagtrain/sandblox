using Sandbox.UI;

namespace Cubism
{
	public partial class HudEntity : Sandbox.HudEntity<RootPanel>
	{
		public HudEntity()
		{
			if ( IsClient )
			{
				RootPanel.StyleSheet.Load( "/Hud.scss" );
			}
		}
	}
}
