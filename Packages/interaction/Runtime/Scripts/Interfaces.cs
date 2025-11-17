using UnityEngine;

namespace Nianyi.UnityPack
{
	public interface IFocusable
	{
		public void OnFocus();
		public void OnLoseFocus();
	}

	public interface ISelectable
	{
		public void OnSelect();
		public void OnDeselect();
	}

	public interface IInteractable
	{
		public void OnInteract();
	}
}
