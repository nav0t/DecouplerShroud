using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DecouplerShroud {
	class Cylinder {

		public float bottomStart = 0;
		public float height = 1;
		public float botRad = 1.25f;
		public float topRad = 1.25f;
		public float uvBot = 0;
		public float uvTop = 1;
		public int sides = 24;
		public float tiling = 1;

		public Cylinder(int sides) {
			this.sides = sides;
		}
		public Cylinder(int sides, float bottomStart, float height, float botRad, float topRad, float uvBot, float uvTop) {
			this.bottomStart = bottomStart;
			this.height = height;
			this.botRad = botRad;
			this.topRad = topRad;
			this.uvBot = uvBot;
			this.uvTop = uvTop;
			this.sides = sides;
		}

		//Basically the same as GenerateCylinders but without triangle generation
		public void UpdateCylinder(int indexOffset, Vector3[] verts, Vector3[] nors, Vector4[] tans, Vector2[] uvs) {

			int res = sides + 1;
			int vertOffset = indexOffset * 2 * res;

			for (int i = 0; i < res; i++) {
				float ang = i / (float)(sides) * 2 * Mathf.PI;
				Vector3 pos = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang));
				Vector3 tan = (new Vector3(-Mathf.Sin(ang), 0, Mathf.Cos(ang))).normalized;
				Vector3 tanv = Vector3.up * height - pos * (botRad - topRad);

				Vector3 nor = -Vector3.Cross(tan, tanv).normalized;

				verts[vertOffset + 2 * i + 0] = pos * botRad + Vector3.up * bottomStart;
				verts[vertOffset + 2 * i + 1] = pos * topRad + Vector3.up * (height + bottomStart);

				nors[vertOffset + 2 * i + 0] = nor;
				nors[vertOffset + 2 * i + 1] = nor;

				uvs[vertOffset + 2 * i + 0] = new Vector2(tiling * i / (float)sides, uvBot);
				uvs[vertOffset + 2 * i + 1] = new Vector2(tiling * i / (float)sides, uvTop);

				Vector4 tan4 = tan;
				tan4.w = (Vector3.Dot(Vector3.Cross(nor, tan), tanv) < 0.0f) ? -1.0f : 1.0f; ;
				tans[vertOffset + 2 * i + 0] = tan4;
				tans[vertOffset + 2 * i + 1] = tan4;
			}
		}

		//Generates meshdata for a single Cylinder, indexOffset used to have multiple cylinders in same mesh
		public void GenerateCylinders(int indexOffset, Vector3[] verts, Vector3[] nors, Vector4[] tans, Vector2[] uvs, int[] tris) {

			int res = sides + 1;
			int vertOffset = indexOffset * 2 * res;
			int trisOffset = indexOffset * 6 * res;

			for (int i = 0; i < res; i++) {
				float ang = i / (float)(sides) * 2 * Mathf.PI;
				Vector3 pos = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang));
				Vector3 tan = (new Vector3(-Mathf.Sin(ang), 0, Mathf.Cos(ang))).normalized;
				Vector3 tanv = Vector3.up * height - pos * (botRad - topRad);

				Vector3 nor = -Vector3.Cross(tan, tanv).normalized;

				verts[vertOffset + 2 * i + 0] = pos * botRad + Vector3.up * bottomStart;
				verts[vertOffset + 2 * i + 1] = pos * topRad + Vector3.up * (height + bottomStart);

				nors[vertOffset + 2 * i + 0] = nor;
				nors[vertOffset + 2 * i + 1] = nor;

				uvs[vertOffset + 2 * i + 0] = new Vector2(tiling * i / (float)sides, uvBot);
				uvs[vertOffset + 2 * i + 1] = new Vector2(tiling * i / (float)sides, uvTop);

				Vector4 tan4 = tan;
				tan4.w = (Vector3.Dot(Vector3.Cross(nor, tan), tanv) < 0.0f) ? -1.0f : 1.0f; ;
				tans[vertOffset + 2 * i + 0] = tan4;
				tans[vertOffset + 2 * i + 1] = tan4;

				tris[trisOffset + 6 * i + 0] = vertOffset + (2 * i + 0) % (2 * res);
				tris[trisOffset + 6 * i + 1] = vertOffset + (2 * i + 1) % (2 * res);
				tris[trisOffset + 6 * i + 2] = vertOffset + (2 * i + 2) % (2 * res);

				tris[trisOffset + 6 * i + 3] = vertOffset + (2 * i + 3) % (2 * res);
				tris[trisOffset + 6 * i + 4] = vertOffset + (2 * i + 2) % (2 * res);
				tris[trisOffset + 6 * i + 5] = vertOffset + (2 * i + 1) % (2 * res);

			}
		}
	}
}
