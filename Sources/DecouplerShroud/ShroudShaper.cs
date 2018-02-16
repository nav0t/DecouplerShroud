using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecouplerShroud {
	class ShroudShaper {

		public int sides;
		public MultiCylinder multiCylinder;
		ModuleDecouplerShroud decouplerShroud;
		float vertOffset, height, botWidth, topWidth, thickness, bottomEdgeSize, topBevelSize, antiZFightSizeIncrease;

		public ShroudShaper(ModuleDecouplerShroud decouplerShroud, int sides) {
			this.sides = sides;
			this.decouplerShroud = decouplerShroud;
			this.multiCylinder = new MultiCylinder(sides, 5);
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
			multiCylinder.GenerateCylinders();
			update();
		}

		public void update() {
			getDecouplerShroudValues();

			topWidth += antiZFightSizeIncrease;
			botWidth += antiZFightSizeIncrease;

			float maxWidth = (topWidth > botWidth)? topWidth : botWidth;
			float widthDiff = (topWidth > botWidth) ? topWidth - botWidth : botWidth - topWidth;
			Cylinder c0 = multiCylinder.cylinders[0]; //outside shell
			Cylinder c1 = multiCylinder.cylinders[1]; //top flat bit
			Cylinder c2 = multiCylinder.cylinders[2]; //inside
			Cylinder c3 = multiCylinder.cylinders[3]; //bottom tucked in edge
			Cylinder c4 = multiCylinder.cylinders[4]; //top bevel in edge

			//Creates bottom edge
			c3.bottomStart = vertOffset - bottomEdgeSize;
			c3.height = bottomEdgeSize;
			c3.botWidth = botWidth - bottomEdgeSize;
			c3.topWidth = botWidth;
			c3.tiling = 2;
			c3.uvBot = 0;
			c3.uvTop = .1f;

			//Sets outer shell values
			c0.bottomStart = vertOffset;
			c0.height = height - topBevelSize;
			c0.botWidth = botWidth;
			c0.topWidth = topWidth;
			c0.tiling = 2;
			c0.uvBot = 0.1f;
			c0.uvTop = 240 / 512f - .1f;
			//c0.uvTop = (height) / (6*maxWidth / c0.tiling);
			//if (c0.uvTop > 240 / 512f) {
			//}

			//Set top Bevel 
			c4.bottomStart = vertOffset + height - topBevelSize;
			c4.height = topBevelSize;
			c4.botWidth = topWidth;
			c4.topWidth = topWidth - topBevelSize;
			c4.tiling = 2;
			c4.uvBot = 240 / 512f - .1f;
			c4.uvTop = 240 / 512f;

			//Sets top bit values
			c1.bottomStart = vertOffset+height;
			c1.height = 0;
			c1.botWidth = topWidth - topBevelSize;
			c1.topWidth = (topWidth - topBevelSize) - thickness * topWidth;
			c1.uvBot = (241 / 512f);
			c1.uvTop = ((512-241) / 512f);
			c1.tiling = ((int)(6*(30 / 512f) / (thickness * topWidth) * (6 * topWidth))) / 6f;

			//Sets inner shell values
			c2.bottomStart = vertOffset + height;
			c2.height = -height;
			c2.botWidth = topWidth - thickness * topWidth;
			c2.topWidth = botWidth - thickness * topWidth;
			c2.uvBot = ((512 - 240) / 512f);
			c2.uvTop = 1;


			topWidth -= antiZFightSizeIncrease;
			botWidth -= antiZFightSizeIncrease;

			//Updates mesh
			multiCylinder.UpdateCylinders();
		}
	}
}
