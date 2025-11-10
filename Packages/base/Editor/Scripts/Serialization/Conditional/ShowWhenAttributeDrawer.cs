using UnityEditor;

namespace Nianyi.UnityPack
{
	[CustomPropertyDrawer(typeof(ShowWhenAttribute))]
	public class ShowWhenAttributeDrawer : ConditionalShowingAttributeDrawer
	{
	}
}