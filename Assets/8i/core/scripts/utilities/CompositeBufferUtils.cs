using UnityEngine;

namespace HVR.Utils
{
	public static class CompositeBufferUtils
	{
		public static Mesh GenerateQuad()
		{
			Vector3[] vertices = new Vector3[4] {
                new Vector3( 1.0f, 1.0f, 0.0f),
                new Vector3(-1.0f, 1.0f, 0.0f),
                new Vector3(-1.0f,-1.0f, 0.0f),
                new Vector3( 1.0f,-1.0f, 0.0f),
            };
			int[] indices = new int[6] { 0, 1, 2, 2, 3, 0 };

			Mesh r = new Mesh();
			r.vertices = vertices;
			r.triangles = indices;
			return r;
		}

		public static Mesh GenerateDetailedQuad()
		{
			const int div_x = 325;
			const int div_y = 200;

			var cell = new Vector2(2.0f / div_x, 2.0f / div_y);
			var vertices = new Vector3[65000];
			var indices = new int[(div_x - 1) * (div_y - 1) * 6];
			for (int iy = 0; iy < div_y; ++iy)
			{
				for (int ix = 0; ix < div_x; ++ix)
				{
					int i = div_x * iy + ix;
					vertices[i] = new Vector3(cell.x * ix - 1.0f, cell.y * iy - 1.0f, 0.0f);
				}
			}
			for (int iy = 0; iy < div_y - 1; ++iy)
			{
				for (int ix = 0; ix < div_x - 1; ++ix)
				{
					int i = ((div_x - 1) * iy + ix) * 6;
					indices[i + 0] = (div_x * (iy + 1)) + (ix + 1);
					indices[i + 1] = (div_x * (iy + 0)) + (ix + 1);
					indices[i + 2] = (div_x * (iy + 0)) + (ix + 0);

					indices[i + 3] = (div_x * (iy + 0)) + (ix + 0);
					indices[i + 4] = (div_x * (iy + 1)) + (ix + 0);
					indices[i + 5] = (div_x * (iy + 1)) + (ix + 1);
				}
			}

			Mesh r = new Mesh();
			r.vertices = vertices;
			r.triangles = indices;
			return r;
		}
	}

}
