using UnityEngine;

namespace Nianyi.UnityPack
{
	public class AlwaysFaceCamera : MonoBehaviour
	{
		public bool reversed = true;

		void LateUpdate()
		{
			Vector3 delta = Camera.main.transform.position - transform.position;
			if(reversed)
				delta = -delta;
			var orientation = Quaternion.LookRotation(delta);
			transform.rotation = orientation;
		}
	}
}
