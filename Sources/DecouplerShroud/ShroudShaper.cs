using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DecouplerShroud {
	class ShroudShaper {

		public int sides;
		public MultiCylinder multiCylinder;
		public MultiCylinder collCylinder;

		ModuleDecouplerShroud decouplerShroud;
		float vertOffset, height, botWidth, topWidth, thickness, bottomEdgeSize, topBevelSize, antiZFightSizeIncrease;
		int outerEdgeLoops, topEdgeLoops;

		public ShroudShaper(ModuleDecouplerShroud decouplerShroud, int sides) {
			this.sides = sides;
			this.decouplerShroud = decouplerShroud;
			multiCylinder = new MultiCylinder(sides, 6, 3);
			collCylinder = new MultiCylinder(sides, 2, 1);
		}
		
		void getDecouplerShroudValues() {
			this.vertOffset = decouplerShroud.vertOffset;
			this.height = decouplerShroud.height;
			this.botWidth = decouplerShroud.botWidth;
			this.topWidth = decouplerShroud.topWidth;
			this.thickness = decouplerShroud.thickness;
			this.bottomEdgeSize = decouplerShroud.bottomEdgeSize;
			this.topBevelSize = decouplerShroud.topBevelSize;
			this.antiZFightSizeIncrease = decouplerShroud.antiZFightSizeIncrease;
			this.outerEdgeLoops = decouplerShroud.outerEdgeLoops;
			this.topEdgeLoops = decouplerShroud.topEdgeLoops;

			multiCylinder.segments = decouplerShroud.segments;
			collCylinder.segments = decouplerShroud.segments * decouplerShroud.collPerSegment;
		}

		public void generate() {
			setCylinderValues();
			multiCylinder.generateMeshes();
			collCylinder.generateMeshes();
		}

		public void update() {
			setCylinderValues();

			//Updates mesh
			multiCylinder.updateMeshes();
			collCylinder.updateMeshes();

		}

		public void setCylinderValues() {
			getDecouplerShroudValues();

			topWidth += antiZFightSizeIncrease;
			botWidth += antiZFightSizeIncrease;

			float maxWidth = (topWidth > botWidth) ? topWidth : botWidth;
			float widthDiff = (topWidth > botWidth) ? topWidth - botWidth : botWidth - topWidth;
			Cylinder c0 = multiCylinder.cylinders[0]; //outside shell
			Cylinder c1 = multiCylinder.cylinders[1]; //top flat bit
			Cylinder c2 = multiCylinder.cylinders[2]; //inside
			Cylinder c3 = multiCylinder.cylinders[3]; //bottom tucked in edge
			Cylinder c4 = multiCylinder.cylinders[4]; //top bevel in edge
			Cylinder c5 = multiCylinder.cylinders[5]; //bottom flat

			Cylinder coll0 = collCylinder.cylinders[0];//Collision outer
			Cylinder coll1 = collCylinder.cylinders[1];//Collision inner

			Vector2 bevel = new Vector2((botWidth - topWidth), height).normalized + Vector2.right;
			bevel = bevel.normalized * topBevelSize;

			//Creates bottom edge
			c3.submesh = 0;
			c3.bottomStart = vertOffset - bottomEdgeSize;
			c3.height = bottomEdgeSize;
			c3.botWidth = botWidth - bottomEdgeSize;
			c3.topWidth = botWidth;
			c3.uvBot = 0;
			c3.uvTop = .01f;

			//Sets outer shell values
			c0.submesh = 0;
			c0.bottomStart = vertOffset;
			c0.height = height - bevel.y;
			c0.botWidth = botWidth;
			c0.topWidth = topWidth;
			c0.uvBot = 0.01f;
			c0.uvTop = 1 - .01f;
			c0.rings = outerEdgeLoops;
			//c0.uvTop = (height) / (6*maxWidth / c0.tiling);
			//if (c0.uvTop > 240 / 512f) {
			//}

			//Set top Bevel 
			c4.submesh = 0;
			c4.bottomStart = vertOffset + height - bevel.y;
			c4.height = bevel.y;
			c4.botWidth = topWidth;
			c4.topWidth = topWidth - bevel.x;
			c4.uvBot = 1 - .01f;
			c4.uvTop = 1;

			//Sets top bit values
			c1.submesh = 1;
			c1.bottomStart = vertOffset + height;
			c1.height = 0;
			c1.botWidth = topWidth - bevel.x;
			c1.topWidth = (topWidth - bevel.x) - thickness * topWidth;
			c1.uvBot = 0;
			c1.uvTop = 1;
			c1.rings = topEdgeLoops;

			//Sets bottom flat bit
			c5.submesh = 1;
			c5.bottomStart = vertOffset - bottomEdgeSize;
			c5.height = 0;
			c5.topWidth = botWidth - bottomEdgeSize;
			c5.botWidth = Mathf.Min(botWidth - thickness * topWidth, botWidth - bottomEdgeSize);
			c5.uvBot = 0;
			c5.uvTop = 1;
			c5.rings = topEdgeLoops;

			//Sets inner shell values
			c2.submesh = 2;
			c2.bottomStart = vertOffset + height;
			c2.height = -height - bottomEdgeSize;
			c2.botWidth = topWidth - thickness * topWidth;
			c2.topWidth = Mathf.Min(botWidth - thickness * topWidth, botWidth - bottomEdgeSize);
			c2.uvBot = 0;
			c2.uvTop = 1;

			coll0.submesh = 0;
			coll0.bottomStart = vertOffset;
			coll0.height = height - bevel.y;
			coll0.botWidth = botWidth;
			coll0.topWidth = topWidth;
			coll1.uvBot = 0;
			coll1.uvTop = 1;

			coll1.submesh = 0;
			coll1.bottomStart = vertOffset + height;
			coll1.height = -height - bottomEdgeSize;
			coll1.botWidth = topWidth - thickness * topWidth / 4f;
			coll1.topWidth = Mathf.Min(botWidth - thickness * topWidth / 4f, botWidth - bottomEdgeSize);
			coll1.uvBot = 0;
			coll1.uvTop = 1;

			topWidth -= antiZFightSizeIncrease;
			botWidth -= antiZFightSizeIncrease;
		}
	}
}
