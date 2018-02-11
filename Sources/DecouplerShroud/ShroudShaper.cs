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

		public ShroudShaper(int sides, float bottomStart, float height, float botRad, float topRad) {

			this.sides = sides;
			this.multiCylinder = new MultiCylinder(sides, 3);
		}
		
		public void generate(float bottomStart, float height, float botRad, float topRad, float width) {
			multiCylinder.GenerateCylinders();
			update(bottomStart, height, botRad, topRad, width);
		}

		public void update(float bottomStart, float height, float botRad, float topRad, float width) {
			float maxRad = (topRad > botRad)? topRad : botRad;
			Cylinder c0 = multiCylinder.cylinders[0];
			Cylinder c1 = multiCylinder.cylinders[1];
			Cylinder c2 = multiCylinder.cylinders[2];

			//Sets outer shell values
			c0.bottomStart = bottomStart;
			c0.height = height;
			c0.botRad = botRad;
			c0.topRad = topRad;
			c0.uvBot = 0;
			c0.uvTop = 2*height / (6*maxRad);
			if (c0.uvTop > 240 / 512f) {
				c0.uvTop = 240 / 512f;
			}
			c0.tiling = 2;
			
			//Sets top bit values
			c1.bottomStart = bottomStart+height;
			c1.height = 0;
			c1.botRad = topRad;
			c1.topRad = topRad - width * topRad;
			c1.uvBot = (241 / 512f);
			c1.uvTop = ((512-241) / 512f);
			c1.tiling = ((int)(6*(30 / 512f) / (width * topRad) * (6 * topRad))) / 6f;

			//Sets inner shell values
			c2.bottomStart = bottomStart + height;
			c2.height = -height;
			c2.botRad = topRad - width * topRad;
			c2.topRad = botRad - width * topRad;
			c2.uvBot = ((512 - 240) / 512f);
			c2.uvTop = 1;

			//Updates mesh
			multiCylinder.UpdateCylinders();
		}
	}
}
