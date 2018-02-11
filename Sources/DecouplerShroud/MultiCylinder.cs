using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DecouplerShroud {
	class MultiCylinder {
		public int faces;
		public Cylinder[] cylinders;

		public Mesh mesh;

		Vector3[] verts;
		Vector3[] nors;
		Vector4[] tans;
		Vector2[] uvs;
		int[] tris;

		public MultiCylinder(int faces, int cylinderCount) {
			this.faces = faces;
			this.cylinders = new Cylinder[cylinderCount];

			for (int i = 0; i < cylinders.Length; i++) {
				this.cylinders[i] = new Cylinder(faces);
			}

			this.mesh = new Mesh();
			
		}

		//Generate mesh for the first time
		public void GenerateCylinders() {

			int res = faces + 1;

			verts = new Vector3[res * 2 * cylinders.Length];
			nors = new Vector3[res * 2 * cylinders.Length];
			tans = new Vector4[res * 2 * cylinders.Length];
			uvs = new Vector2[res * 2 * cylinders.Length];
			tris = new int[res * 6 * cylinders.Length];

			for (int i = 0; i < cylinders.Length; i++) {
				cylinders[i].GenerateCylinders(i, verts, nors, tans, uvs, tris);
			}

			mesh.vertices = verts;
			mesh.triangles = tris;
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
