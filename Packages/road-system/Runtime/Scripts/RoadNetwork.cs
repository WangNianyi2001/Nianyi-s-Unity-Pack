using UnityEngine;

namespace Nianyi.UnityPack.RoadSystem
{
	public partial class RoadNetwork : MonoBehaviour
	{
		[SerializeField] RoadNetworkAsset roadNetworkAsset;

		public RoadNetworkData Data
		{
			get
			{
				if(roadNetworkAsset == null)
					return null;
				return roadNetworkAsset.data;
			}
		}
	}
}
