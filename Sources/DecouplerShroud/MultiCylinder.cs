using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DecouplerShroud {
	class MultiCylinder {
		public int faces;
		public int subMeshCount;
		public Cylinder[] cylinders;

		public Mesh mesh;

		Vector3[] verts;
		Vector3[] nors;
		Vector4[] tans;
		Vector2[] uvs;
		List<int>[] tris;

		public MultiCylinder(int faces, int cylinderCount, int subMeshCount) {
			this.faces = faces;
			this.subMeshCount = subMeshCount;
			this.cylinders = new Cylinder[cylinderCount];

			for (int i = 0; i < cylinders.Length; i++) {
				this.cylinders[i] = new Cylinder(faces);
			}

			this.mesh = new Mesh();
			this.mesh.subMeshCount = subMeshCount;
		}

		//Generate mesh for the first time
		public void GenerateCylinders() {

			int res = faces + 1;

			verts = new Vector3[res * 2 * cylinders.Length];
			nors = new Vector3[res * 2 * cylinders.Length];
			tans = new Vector4[res * 2 * cylinders.Length];
			uvs = new Vector2[res * 2 * cylinders.Length];
			tris = new List<int>[subMeshCount];
			for (int i = 0; i < subMeshCount; i++) {
				tris[i] = new List<int>();
			}

			for (int i = 0; i < cylinders.Length; i++) {
				cylinders[i].GenerateCylinders(i, verts, nors, tans, uvs, tris);
			}

			mesh.vertices = verts;
			for (int i = 0; i < subMeshCount; i++) {
				mesh.SetTriangles(tris[i], i);
			}
			mesh.uv = uvs;
			mesh.normals = nors;
			mesh.tangents = tans;
			mesh.RecalculateBounds();
		}

		//Update already created mesh
		public void UpdateCylinders() {

			for (int i = 0; i < cylinders.Length; i++) {
				cylinders[i].UpdateCylinder(i, verts, nors, tans, uvs);
			}

			mesh.vertices = verts;
			mesh.uv = uvs;
			mesh.normals = nors;
			mesh.tangents = tans;
			mesh.RecalculateBounds();
		}

	}
}
