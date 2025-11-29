using UnityEngine;
using Nianyi.UnityPack;

[RequireComponent(typeof(RectTransform))]
public class OnscreenPointer : MonoBehaviour
{
	public Transform target;
	Canvas canvas;

	protected void Start()
	{
		canvas = GetComponentInParent<Canvas>();
	}

	protected void LateUpdate()
	{
		Camera cam = Camera.main;
		if(!target || !canvas || !cam)
			return;

		float hw = Screen.width / 2, hh = Screen.height / 2;
		Vector2 bias = new(hw, hh);
		Vector3 screenPos = cam.WorldToScreenPoint(target.position);
		Vector2 xy = (Vector2)screenPos - bias;

		if(!target.IsOnScreen())
		{
			if(screenPos.z <= 0)
				xy = -xy;
			if(xy.sqrMagnitude == 0)
				xy = Vector2.down;
			float maxMagnitude = bias.magnitude;
			xy *= maxMagnitude / xy.magnitude;

			float rx = hw / Mathf.Abs(xy.x);
			if(rx < 1)
				xy *= rx;
			float ry = hh / Mathf.Abs(xy.y);
			if(ry < 1)
				xy *= ry;
		}

		transform.position = canvas.transform.localToWorldMatrix.MultiplyPoint(xy);
		float rot = Mathf.Atan2(-xy.x, xy.y);
		transform.rotation = canvas.transform.rotation * Quaternion.Euler(0, 0, rot * Mathf.Rad2Deg);
	}
}
