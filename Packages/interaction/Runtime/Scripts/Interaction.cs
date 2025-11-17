using UnityEngine;
using UnityEngine.Events;

namespace Nianyi.UnityPack
{
	public class Interaction : MonoBehaviour, IFocusable, ISelectable, IInteractable
	{
		[Header("Focus")]

		public UnityEvent onFocus;
		public UnityEvent onLoseFocus;

		public void OnFocus()
		{
			onFocus?.Invoke();
		}

		public void OnLoseFocus()
		{
			onLoseFocus?.Invoke();
		}

		[Header("Select")]

		public UnityEvent onSelect;
		public UnityEvent onDeselect;

		public void OnSelect()
		{
			onSelect?.Invoke();
		}

		public void OnDeselect()
		{
			onDeselect?.Invoke();
		}

		[Header("Interact")]

		public UnityEvent onInteract;

		public void OnInteract()
		{
			onInteract?.Invoke();
		}
	}
}
