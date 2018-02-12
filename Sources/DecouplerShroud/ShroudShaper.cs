using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecouplerShroud {
	class ShroudShaper {

		public int sides;
		public MultiCylinder multiCylinder;

		public ShroudShaper(int sides) {
			this.sides = sides;
			this.multiCylinder = new MultiCylinder(sides, 3);
		}

		public ShroudShaper(int sides, float bottomStart, float height, float botWidth, float topWidth) {

			this.sides = sides;
			this.multiCylinder = new MultiCylinder(sides, 3);
		}
		
		public void generate(float bottomStart, float height, float botWidth, float topWidth, float thickness) {
			multiCylinder.GenerateCylinders();
			update(bottomStart, height, botWidth, topWidth, thickness);
		}

		public void update(float bottomStart, float height, float botWidth, float topWidth, float thickness) {
			float maxWidth = (topWidth > botWidth)? topWidth : botWidth;
			float widthDiff = (topWidth > botWidth) ? topWidth - botWidth : botWidth - topWidth;
			Cylinder c0 = multiCylinder.cylinders[0];
			Cylinder c1 = multiCylinder.cylinders[1];
			Cylinder c2 = multiCylinder.cylinders[2];

			//Sets outer shell values
			c0.bottomStart = bottomStart;
			c0.height = height;
			c0.botWidth = botWidth;
			c0.topWidth = topWidth;
			c0.tiling = 2;
			c0.uvBot = 0;
			c0.uvTop = (height) / (6*maxWidth / c0.tiling);
			if (c0.uvTop > 240 / 512f) {
			}
			c0.uvTop = 240 / 512f;
			
			//Sets top bit values
			c1.bottomStart = bottomStart+height;
			c1.height = 0;
			c1.botWidth = topWidth;
			c1.topWidth = topWidth - thickness * topWidth;
			c1.uvBot = (241 / 512f);
			c1.uvTop = ((512-241) / 512f);
			c1.tiling = ((int)(6*(30 / 512f) / (thickness * topWidth) * (6 * topWidth))) / 6f;

			//Sets inner shell values
			c2.bottomStart = bottomStart + height;
			c2.height = -height;
			c2.botWidth = topWidth - thickness * topWidth;
			c2.topWidth = botWidth - thickness * topWidth;
			c2.uvBot = ((512 - 240) / 512f);
			c2.uvTop = 1;

			//Updates mesh
			multiCylinder.UpdateCylinders();
		}
	}
}
