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

			int ringCount = 0;

			foreach (Cylinder c in cylinders) {
				ringCount += c.rings;
			}

			verts = new Vector3[res * ringCount];
			nors = new Vector3[res * ringCount];
			tans = new Vector4[res * ringCount];
			uvs = new Vector2[res * ringCount];
			tris = new List<int>[subMeshCount];
			for (int i = 0; i < subMeshCount; i++) {
				tris[i] = new List<int>();
			}

			int ringOffset = 0;

			for (int i = 0; i < cylinders.Length; i++) {
				cylinders[i].GenerateCylinders(ringOffset, verts, nors, tans, uvs, tris);
				ringOffset += cylinders[i].rings;
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

			int ringOffset = 0;
			for (int i = 0; i < cylinders.Length; i++) {
				cylinders[i].UpdateCylinder(ringOffset, verts, nors, tans, uvs);
				ringOffset += cylinders[i].rings;
			}

			mesh.vertices = verts;
			mesh.uv = uvs;
			mesh.normals = nors;
			mesh.tangents = tans;
			mesh.RecalculateBounds();
		}

	}
}
