using UnityEditor;

namespace Nianyi.UnityPack.Editor
{
	[CustomPropertyDrawer(typeof(ShowWhenAttribute))]
	public class ShowWhenAttributeDrawer : ConditionalShowingAttributeDrawer
	{
	}
}