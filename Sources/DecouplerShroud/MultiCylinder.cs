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
		public bool hasSides = false;
		public Cylinder[] cylinders;

		public Mesh[] meshes;

		public int segments = 1;

		Vector3[] verts;
		Vector3[] nors;
		Vector4[] tans;
		Vector2[] uvs;
		List<int>[] tris;

		public MultiCylinder(int faces, int cylinderCount, int subMeshCount, bool generateSides) {
			this.faces = faces;
			this.subMeshCount = subMeshCount;
			this.hasSides = generateSides;
			this.cylinders = new Cylinder[cylinderCount];

			for (int i = 0; i < cylinders.Length; i++) {
				this.cylinders[i] = new Cylinder(faces);
			}
		}

		void setupMeshes() {
			this.meshes = new Mesh[segments];
			for (int i = 0; i < segments; i++) {
				meshes[i] = new Mesh();
				meshes[i].subMeshCount = subMeshCount;
			}
		}

		public void generateMeshes() {
			setupMeshes();
			for (int i = 0; i < segments; i++) {
				GenerateCylinders(i);
			}
		}
		public void updateMeshes() {
			for (int i = 0; i < segments; i++) {
				UpdateCylinders(i);
			}
		}

		//Generate mesh for the first time
		public void GenerateCylinders(int segmentIndex) {
			Mesh mesh = meshes[segmentIndex];
			int res = faces / segments + 1;

			int ringCount = 0;

			foreach (Cylinder c in cylinders) {
				c.segments = segments;
				c.startDeg = -.25f + (segmentIndex) / (float)segments;
				ringCount += c.rings;
			}

			int vertCount = res * ringCount;
			if (hasSides && segments > 1) {
				vertCount += cylinders.Length * 2;
			}
			verts = new Vector3[vertCount];
			nors = new Vector3[vertCount];
			tans = new Vector4[vertCount];
			uvs = new Vector2[vertCount];
			tris = new List<int>[subMeshCount];
			for (int i = 0; i < subMeshCount; i++) {
				tris[i] = new List<int>();
			}

			int ringOffset = 0;

			//Generate Arrays with mesh info
			for (int i = 0; i < cylinders.Length; i++) {
				cylinders[i].GenerateCylinders(ringOffset, verts, nors, tans, uvs, tris);
				ringOffset += cylinders[i].rings;
			}

			//Generate the sides
			if (hasSides && segments > 1) {
				float ang1 = (-.25f + segmentIndex / (float)segments) * 2 * Mathf.PI;
				float ang2 = (-.25f + segmentIndex / (float)segments + 1 / (float) segments) * 2 * Mathf.PI;
				GenerateSides(verts, uvs, tris[tris.Length - 1], nors, ang1, ang2);
			}

			//Assign arrays to mesh
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
		public void UpdateCylinders(int segmentIndex) {
			Mesh mesh = meshes[segmentIndex];
			if (mesh == null) {
				Debug.LogError("Decoupler Shroud: Mesh  "+segmentIndex+" of "+segments+" segments is null");
				return;
			}

			int ringOffset = 0;
			for (int i = 0; i < cylinders.Length; i++) {
				cylinders[i].segments = segments;
				cylinders[i].startDeg = -.25f + (segmentIndex) / (float)segments;

				cylinders[i].UpdateCylinder(ringOffset, verts, nors, tans, uvs);
				ringOffset += cylinders[i].rings;
			}

			//Generate the sides
			if (hasSides && segments > 1) {
				float ang1 = (-.25f + segmentIndex / (float)segments) * 2 * Mathf.PI;
				float ang2 = (-.25f + segmentIndex / (float)segments + 1 / (float)segments) * 2 * Mathf.PI;
				UpdateSides(verts, ang1, ang2);
			}

			mesh.vertices = verts;
			mesh.uv = uvs;
			mesh.normals = nors;
			mesh.tangents = tans;
			mesh.RecalculateBounds();
		}

		public void GenerateSides(Vector3[] verts, Vector2[] uvs, List<int> tris, Vector3[] nors, float ang1, float ang2) {

			int vCount = cylinders.Length;
			int vStart1 = verts.Length - vCount * 2;
			int vStart2 = vStart1 + vCount;
			for (int i = 0; i < vCount; i++) {
				nors[vStart1 + i] = new Vector3(Mathf.Cos(ang1), 0, -Mathf.Sin(ang1));
				nors[vStart2 + i] = -new Vector3(Mathf.Cos(ang2), 0, -Mathf.Sin(ang2));
			}

			
			for (int i = 0; i < (vCount+1) / 2 - 1; i++) {
				tris.Add(vStart1 + ((vCount - i) % vCount));
				tris.Add(vStart1 + i + 1);
				tris.Add(vStart1 + vCount - i - 1);

				tris.Add(vStart1 + i + 1);
				tris.Add(vStart1 + i + 2);
				tris.Add(vStart1 + vCount - i - 1);

				tris.Add(vStart2 + i + 1);
				tris.Add(vStart2 + ((vCount - i) % vCount));
				tris.Add(vStart2 + vCount - i - 1);

				tris.Add(vStart2 + i + 2);
				tris.Add(vStart2 + i + 1);
				tris.Add(vStart2 + vCount - i - 1);
			}
			UpdateSides(verts, ang1, ang2);

		}

		public void UpdateSides(Vector3[] verts, float ang1, float ang2) {
			int vCount = cylinders.Length;
			int vStart1 = verts.Length - vCount * 2;
			int vStart2 = vStart1 + vCount;
			for (int i = 0; i < vCount; i++) {
				verts[vStart1 + i] = new Vector3(Mathf.Cos(ang1), 0, Mathf.Sin(ang1)) * cylinders[i].botWidth / 2 + Vector3.up * cylinders[i].bottomStart;
				verts[vStart2 + i] = new Vector3(Mathf.Cos(ang2), 0, Mathf.Sin(ang2)) * cylinders[i].botWidth / 2 + Vector3.up * cylinders[i].bottomStart;
			}
		}
	}
}
