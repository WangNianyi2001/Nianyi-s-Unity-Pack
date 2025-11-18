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
}
