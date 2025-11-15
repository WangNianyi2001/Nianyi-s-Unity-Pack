using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Nianyi.UnityPack
{
	[ExecuteAlways]
	public abstract class ProceduralGenerator : MonoBehaviour
	{
		#region Unity life cycle
		protected void Awake()
		{
#if UNITY_EDITOR
			if(!Application.isPlaying)
			{
				Regenerate();
				return;
			}
#endif
			Ungarrison();
		}
		#endregion

		#region Generation
		public void Ungarrison()
		{
			Regenerate();
			HierarchyUtility.Destroy(this);
		}

		public abstract void Regenerate();
		#endregion
	}

	/// <remarks>
	/// When <c>config</c> is modified in the editor, <c>Regenerate</c> automatically gets triggered.
	/// In most cases you should derive from <c>ProceduralGenerator<Config></c> to get automatic regeneration.
	/// Doesn't work well with "Project Settings/Enter Play Mode Options/Reload Scene" being turned off, as it relies on `Awake` to run.
	/// </remarks>
	/// <typeparam name="Config">
	/// The data structure defining the configuration data of this generator.
	/// </typeparam>
	public abstract class ProceduralGenerator<Config> : ProceduralGenerator
	{
		[SerializeField, Expanded] protected Config config;
	}
}
