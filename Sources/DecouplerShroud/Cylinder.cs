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
		public float botWidth = 1.25f;
		public float topWidth = 1.25f;
		public float uvBot = 0;
		public float uvTop = 1;
		public int sides = 24;
		public int submesh = 0;
		public int rings = 2;

		public float segments = 1;
		public float startDeg = 0;

		public Cylinder(int sides) {
			this.sides = sides;
		}

		//Basically the same as GenerateCylinders but without triangle generation
		public void UpdateCylinder(int ringOffset, Vector3[] verts, Vector3[] nors, Vector4[] tans, Vector2[] uvs) {

			//Last vert needs to be done twice for uv coords
			int res = Mathf.RoundToInt(sides / (float)segments) + 1;

			int vertOffset = ringOffset * res;

			for (int i = 0; i < res; i++) {
				float uVal = startDeg + i / (float)(sides);

				float ang = uVal * 2 * Mathf.PI;

				Vector3 pos = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) / 2;
				Vector3 tan = (new Vector3(-Mathf.Sin(ang), 0, Mathf.Cos(ang))).normalized;
				Vector3 tanv = Vector3.up * height - pos * (botWidth - topWidth);

				Vector3 nor = -Vector3.Cross(tan, tanv).normalized;

				Vector4 tan4 = tan;
				tan4.w = (Vector3.Dot(Vector3.Cross(nor, tan), tanv) < 0.0f) ? -1.0f : 1.0f;

				Vector3 posBot = pos * botWidth + Vector3.up * bottomStart;
				Vector3 posTop = pos * topWidth + Vector3.up * (height + bottomStart);

				for (int r = 0; r < rings; r++) {

					float lerp = (float)r / (float)(rings - 1);
					verts[vertOffset + rings * i + r] = Vector3.Lerp(posBot, posTop, lerp);

					nors[vertOffset + rings * i + r] = nor;

					tans[vertOffset + rings * i + r] = tan4;

				}
			}
		}

		//Generates meshdata for a single Cylinder, indexOffset used to have multiple cylinders in same mesh
		public void GenerateCylinders(int ringOffset, Vector3[] verts, Vector3[] nors, Vector4[] tans, Vector2[] uvs, List<int>[] tris) {

			//Last vert needs to be done twice for uv coords
			int res = Mathf.RoundToInt(sides / segments) + 1;
			int vertOffset = ringOffset * res;

			for (int i = 0; i < res; i++) {

				float uVal = startDeg + i / (float)(sides);

				float ang = uVal * 2 * Mathf.PI;

				Vector3 pos = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) / 2;
				Vector3 tan = (new Vector3(-Mathf.Sin(ang), 0, Mathf.Cos(ang))).normalized;
				Vector3 tanv = Vector3.up * height - pos * (botWidth - topWidth);

				Vector3 nor = -Vector3.Cross(tan, tanv).normalized;

				Vector4 tan4 = tan;
				tan4.w = (Vector3.Dot(Vector3.Cross(nor, tan), tanv) < 0.0f) ? -1.0f : 1.0f;

				Vector3 posBot = pos * botWidth + Vector3.up * bottomStart;
				Vector3 posTop = pos * topWidth + Vector3.up * (height + bottomStart);
				for (int r = 0; r < rings; r++) {

					float lerp = (float)r / (float)(rings - 1);
					verts[vertOffset + rings * i + r] = Vector3.Lerp(posBot, posTop, lerp);

					nors[vertOffset + rings * i + r] = nor;

					tans[vertOffset + rings * i + r] = tan4;

					uvs[vertOffset + rings * i + r] = new Vector2(uVal, Mathf.Lerp(uvBot, uvTop, lerp));

					if (r < rings - 1 && i < res - 1) {
						tris[submesh].Add(vertOffset + r + rings * i + 0);
						tris[submesh].Add(vertOffset + r + rings * i + 1);
						tris[submesh].Add(vertOffset + r + rings * i + rings);

						tris[submesh].Add(vertOffset + r + rings * i + rings + 1);
						tris[submesh].Add(vertOffset + r + rings * i + rings);
						tris[submesh].Add(vertOffset + r + rings * i + 1);
					}
				}
			}
		}
	}
}
