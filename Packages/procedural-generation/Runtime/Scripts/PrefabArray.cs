using UnityEngine;
using System.Collections.Generic;

namespace Nianyi.UnityPack
{
	[System.Serializable]
	public class PrefabArrayConfig
	{
		[AssetOnly] public GameObject prefab = null;
		public Matrix4x4 basis = Matrix4x4.identity;
		public Vector3 relativeOffset = Vector3.zero;
		public Vector3Int count = Vector3Int.one;
	}

	public class PrefabArray : ProceduralGenerator<PrefabArrayConfig>
	{
		#region Generation
		[SerializeField, HideInInspector] List<GameObject> instances = new();

		public override void Regenerate()
		{
			DestroyPreviousGeneration();
			Generate();
		}

		void Generate()
		{
			foreach(var index in EnumerateNaturalCoordinates(config.count))
			{
				Vector4 pos = (Vector3)index - Vector3.Scale(config.relativeOffset, config.count);
				pos.w = 1.0f;
				Vector3 localPosition = config.basis * pos;

				var instance = HierarchyUtility.InstantiateGameObject(config.prefab, transform);
				instance.name = $"{config.prefab.name}@{index}";
				instance.transform.localPosition = localPosition;
				instances.Add(instance);
			}
		}

		void DestroyPreviousGeneration()
		{
			foreach(var child in HierarchyUtility.GetDirectChildren(transform))
				HierarchyUtility.Destroy(child.gameObject);
			instances.Clear();
		}

		IEnumerable<Vector3Int> EnumerateNaturalCoordinates(Vector3Int bounds)
		{
			for(int ix = 0; ix < bounds.x; ++ix)
			{
				for(int iy = 0; iy < bounds.y; ++iy)
				{
					for(int iz = 0; iz < bounds.z; ++iz)
					{
						yield return new(ix, iy, iz);
					}
				}
			}
		}
		#endregion
	}
}
