using RedOnion.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedOnion.KSP.API
{
	[WorkInProgress, Description("Maneuver node.")]
	public class Node
	{
		static Node cachedNext;
		[Description("Next maneuver node of active ship. Null if none or in wrong scene.")]
		public static Node next
		{
			get
			{
				if (!HighLogic.LoadedSceneIsFlight)
					return null;
				var ship = Ship.Active;
				if (ship == null)
					return null;
				var nodes = ship.native.patchedConicSolver?.maneuverNodes;
				if (nodes == null || nodes.Count == 0)
					return null;
				var mnode = nodes[0];
				if (cachedNext == null || cachedNext.native != mnode)
					cachedNext = new Node(ship, mnode);
				return cachedNext;
			}
		}

		[Unsafe, Description("KSP API.")]
		public ManeuverNode native { get; private set; }
		[Description("Ship the node belongs to.")]
		public Ship ship { get; private set; }

		[Description("Create new maneuver node for active ship, specifying time.")]
		public Node(double time, double prograde = 0.0, double normal = 0.0, double radial = 0.0)
			: this(Ship.Active, time, prograde, normal, radial) { }
		[Description("Create new maneuver node for active ship.")]
		public Node(double time, Vector deltav)
			: this(Ship.Active, time, deltav) { }

		protected internal Node(Ship ship, ManeuverNode native)
		{
			this.native = native;
			this.ship = ship;
		}

		protected internal Node(Ship ship, double time, double prograde = 0.0, double normal = 0.0, double radial = 0.0)
		{
			native = ship.native.patchedConicSolver.AddManeuverNode(time);
			this.ship = ship;
			native.DeltaV = new Vector3d(radial, normal, prograde);
		}

		protected internal Node(Ship ship, double time, Vector deltav)
		{
			native = ship.native.patchedConicSolver.AddManeuverNode(time);
			this.ship = ship;
			this.deltav = deltav;
		}

		[Description("Planned time for the maneuver.")]
		public double time => native.UT;
		[Description("Seconds until the maneuver.")]
		public double eta => native.UT - Time.now;

		[Description("Direction and amount of velocity change needed.")]
		public Vector deltav
		{
			get => new Vector(native.GetBurnVector(ship.orbit));
			set
			{
				var vel = ship.velocityAt(time);
				var pos = ship.positionAt(time) - ship.body.position/*At(time)*/;
				var pro = vel.normalized;
				var nrm = vel.cross(pos).normalized;
				var rad = nrm.cross(vel).normalized;

				nativeDeltaV = new Vector3d(value.dot(rad), value.dot(nrm), value.dot(pro));
			}
		}
		[Unsafe, Description("KSP API. Setting it also calls `patchedConicSolver.UpdateFlightPlan()`.")]
		public Vector3d nativeDeltaV
		{
			get => native.DeltaV;
			set
			{
				native.DeltaV = value;
				if (native.attachedGizmo != null)
					native.attachedGizmo.DeltaV = value;
				ship.native.patchedConicSolver.UpdateFlightPlan();
			}
		}


		[Description("Amount of velocity change in prograde direction.")]
		public double prograde
		{
			get => native.DeltaV.z;
			set => nativeDeltaV = new Vector3d(native.DeltaV.x, native.DeltaV.y, value);
		}
		[Description("Amount of velocity change in normal direction.")]
		public double normal
		{
			get => native.DeltaV.y;
			set => nativeDeltaV = new Vector3d(native.DeltaV.x, value, native.DeltaV.z);
		}
		[Description("Amount of velocity change in radial-out direction.")]
		public double radial
		{
			get => native.DeltaV.x;
			set => nativeDeltaV = new Vector3d(value, native.DeltaV.y, native.DeltaV.z);
		}

		[Description("Remove/delete the node.")]
		public void remove()
		{
			if (native == null)
				return;
			native.RemoveSelf();
			native = null;
			ship = null;
		}
		[Description("Remove/delete the node.")]
		public void delete() => remove();
	}
}
