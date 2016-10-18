
using HVR.Interface;

namespace HVR.Core
{
	public class SceneInterface
	{
		private static SceneInterface m_instance;
		public static SceneInterface Self()
		{
			if (m_instance == null)
				m_instance = new SceneInterface();

			return m_instance;
		}

		private HVRSceneInterface m_hvrSceneInterfacee;
		public HVRSceneInterface hvrSceneInterface
		{
			get
			{
				if (m_hvrSceneInterfacee == null || !m_hvrSceneInterfacee.IsValid())
					m_hvrSceneInterfacee = new HVRSceneInterface();

				return m_hvrSceneInterfacee;
			}
		}

		public bool sceneHasPreRendered = false;

		public void Reset()
		{
			sceneHasPreRendered = false;
		}
	}
}
