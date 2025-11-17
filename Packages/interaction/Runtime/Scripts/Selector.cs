using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Nianyi.UnityPack
{
	public abstract class Selector : MonoBehaviour
	{
		#region Configurations
		public bool affectFocusables = true;
		public bool affectSelectables = true;

		public LayerMask layerMask = ~0;
		#endregion

		#region Unity life cycle
		protected void Update()
		{
			UpdateRegistry();
		}
		#endregion

		#region Registry
		readonly HashSet<IFocusable> focused = new();
		readonly HashSet<ISelectable> selected = new();

		public abstract IEnumerable<IFocusable> GetFocusedCandidates();
		public abstract IEnumerable<ISelectable> GetSelectedCandidates();

		void UpdateRegistry()
		{
			IFocusable[] addedFocused = new IFocusable[0], removedFocused = new IFocusable[0];
			ISelectable[] addedSelected = new ISelectable[0], removedSelected = new ISelectable[0];

			if(affectFocusables)
				UpdateRegistry(focused, GetFocusedCandidates(), out addedFocused, out removedFocused);
			if(affectSelectables)
				UpdateRegistry(selected, GetSelectedCandidates(), out addedSelected, out removedSelected);

			if(affectFocusables)
				foreach(var x in addedFocused) x.OnFocus();
			if(affectSelectables)
				foreach(var x in addedSelected) x.OnSelect();
			if(affectSelectables)
				foreach(var x in removedSelected) x.OnDeselect();
			if(affectFocusables)
				foreach(var x in removedFocused) x.OnLoseFocus();
		}

		void UpdateRegistry<T>(ISet<T> registry, IEnumerable<T> updated, out T[] added, out T[] removed) where T : class
		{
			updated = new HashSet<T>(updated);
			added = updated.Where(x => !registry.Contains(x)).ToArray();
			removed = registry.Where(x => !updated.Contains(x)).ToArray();
			foreach(var x in removed) registry.Remove(x);
			foreach(var x in added) registry.Add(x);
		}
		#endregion
	}
}
