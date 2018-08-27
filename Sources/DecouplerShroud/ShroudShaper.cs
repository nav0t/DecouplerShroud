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
		float vertOffset, height, botWidth, topWidth, thickness, bottomEdgeSize, topBevelSize, antiZFightSizeIncrease, collisionThickness;
		int outerEdgeLoops, topEdgeLoops;

		public ShroudShaper(ModuleDecouplerShroud decouplerShroud, int sides) {
			this.sides = sides;
			this.decouplerShroud = decouplerShroud;
			multiCylinder = new MultiCylinder(sides, 6, 3, true);
			collCylinder = new MultiCylinder(sides, 2, 1, false);
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
			this.collisionThickness = decouplerShroud.collisionThickness;

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
			Cylinder cout = multiCylinder.cylinders[0]; //outside shell
			Cylinder ctopBevel = multiCylinder.cylinders[5]; //top bevel in edge
			Cylinder ctop = multiCylinder.cylinders[4]; //top flat bit
			Cylinder inside = multiCylinder.cylinders[3]; //inside
			Cylinder cbot = multiCylinder.cylinders[2]; //bottom flat
			Cylinder cBotBevel = multiCylinder.cylinders[1]; //bottom tucked in edge

			Cylinder coll0 = collCylinder.cylinders[0];//Collision outer
			Cylinder coll1 = collCylinder.cylinders[1];//Collision inner

			Vector2 bevel = new Vector2((botWidth - topWidth), height).normalized + Vector2.right;
			bevel = bevel.normalized * topBevelSize;

			//Creates bottom edge
			cBotBevel.submesh = 0;
			cBotBevel.bottomStart = vertOffset - bottomEdgeSize;
			cBotBevel.height = bottomEdgeSize;
			cBotBevel.botWidth = botWidth - bottomEdgeSize;
			cBotBevel.topWidth = botWidth;
			cBotBevel.uvBot = 0;
			cBotBevel.uvTop = .01f;

			//Sets outer shell values
			cout.submesh = 0;
			cout.bottomStart = vertOffset;
			cout.height = height - bevel.y;
			cout.botWidth = botWidth;
			cout.topWidth = topWidth;
			cout.uvBot = 0.01f;
			cout.uvTop = 1 - .01f;
			cout.rings = outerEdgeLoops;
			//c0.uvTop = (height) / (6*maxWidth / c0.tiling);
			//if (c0.uvTop > 240 / 512f) {
			//}

			//Set top Bevel 
			ctopBevel.submesh = 0;
			ctopBevel.bottomStart = vertOffset + height - bevel.y;
			ctopBevel.height = bevel.y;
			ctopBevel.botWidth = topWidth;
			ctopBevel.topWidth = topWidth - bevel.x;
			ctopBevel.uvBot = 1 - .01f;
			ctopBevel.uvTop = 1;

			//Sets top bit values
			ctop.submesh = 1;
			ctop.bottomStart = vertOffset + height;
			ctop.height = 0;
			ctop.botWidth = topWidth - bevel.x;
			ctop.topWidth = (topWidth - bevel.x) - thickness * topWidth;
			ctop.uvBot = 0;
			ctop.uvTop = 1;
			ctop.rings = topEdgeLoops;

			//Sets bottom flat bit
			cbot.submesh = 1;
			cbot.bottomStart = vertOffset - bottomEdgeSize;
			cbot.height = 0;
			cbot.topWidth = botWidth - bottomEdgeSize;
			cbot.botWidth = Mathf.Min(botWidth - thickness * topWidth, botWidth - bottomEdgeSize);
			cbot.uvBot = 0;
			cbot.uvTop = 1;
			cbot.rings = topEdgeLoops;

			//Sets inner shell values
			inside.submesh = 2;
			inside.bottomStart = vertOffset + height;
			inside.height = -height - bottomEdgeSize;
			inside.botWidth = topWidth - thickness * topWidth;
			inside.topWidth = Mathf.Min(botWidth - thickness * topWidth, botWidth - bottomEdgeSize);
			inside.uvBot = 0;
			inside.uvTop = 1;

			coll0.submesh = 0;
			coll0.bottomStart = vertOffset;
			coll0.height = height - bevel.y;
			coll0.botWidth = botWidth;
			coll0.topWidth = topWidth;
			coll0.uvBot = 0;
			coll0.uvTop = 1;

			coll1.submesh = 0;
			coll1.bottomStart = vertOffset + height;
			coll1.height = -height - bottomEdgeSize;
			coll1.botWidth = topWidth - collisionThickness * thickness * topWidth;
			coll1.topWidth = Mathf.Min(botWidth - collisionThickness * thickness * topWidth, botWidth - bottomEdgeSize);
			coll1.uvBot = 0;
			coll1.uvTop = 1;

			topWidth -= antiZFightSizeIncrease;
			botWidth -= antiZFightSizeIncrease;
		}
	}
}
