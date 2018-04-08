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
		ModuleDecouplerShroud decouplerShroud;
		float vertOffset, height, botWidth, topWidth, thickness, bottomEdgeSize, topBevelSize, antiZFightSizeIncrease;

		public ShroudShaper(ModuleDecouplerShroud decouplerShroud, int sides) {
			this.sides = sides;
			this.decouplerShroud = decouplerShroud;
			this.multiCylinder = new MultiCylinder(sides, 5, 3);
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

		}

		public void generate() {
			setCylinderValues();
			multiCylinder.GenerateCylinders();
		}

		public void update() {
			setCylinderValues();

			//Updates mesh
			multiCylinder.UpdateCylinders();
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

			Vector2 bevel = new Vector2((botWidth - topWidth), height).normalized + Vector2.right;
			bevel = bevel.normalized * topBevelSize;

			//Creates bottom edge
			c3.submesh = 0;
			c3.bottomStart = vertOffset - bottomEdgeSize;
			c3.height = bottomEdgeSize;
			c3.botWidth = botWidth - bottomEdgeSize;
			c3.topWidth = botWidth;
			c3.tiling = 2;
			c3.uvBot = 0;
			c3.uvTop = .01f;

			//Sets outer shell values
			c0.submesh = 0;
			c0.bottomStart = vertOffset;
			c0.height = height - bevel.y;
			c0.botWidth = botWidth;
			c0.topWidth = topWidth;
			c0.tiling = 2;
			c0.uvBot = 0.01f;
			c0.uvTop = 1 - .01f;
			//c0.uvTop = (height) / (6*maxWidth / c0.tiling);
			//if (c0.uvTop > 240 / 512f) {
			//}

			//Set top Bevel 
			c4.submesh = 0;
			c4.bottomStart = vertOffset + height - bevel.y;
			c4.height = bevel.y;
			c4.botWidth = topWidth;
			c4.topWidth = topWidth - bevel.x;
			c4.tiling = 2;
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
			c1.tiling = ((int)(1 / (thickness)));

			//Sets inner shell values
			c2.submesh = 2;
			c2.bottomStart = vertOffset + height;
			c2.height = -height;
			c2.botWidth = topWidth - thickness * topWidth;
			c2.topWidth = botWidth - thickness * topWidth;
			c2.uvBot = 0;
			c2.uvTop = 1;


			topWidth -= antiZFightSizeIncrease;
			botWidth -= antiZFightSizeIncrease;
		}
	}
}
