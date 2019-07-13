using RedOnion.ROS.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using RedOnion.KSP.API;
using System.ComponentModel;
using MoonSharp.Interpreter;
using KSP.UI.Screens;

namespace RedOnion.KSP.Parts
{
	public interface IPartSet : ICollection<PartBase>
	{
		bool Dirty { get; }
		void SetDirty();
		event Action Refresh;
	}
	public class ReadOnlyList<T> : IList<T>
	{
		protected ListCore<T> list;
		protected internal Action Refresh;
		protected internal bool Dirty { get; set; } = true;
		protected internal virtual void SetDirty() => Dirty = true;
		protected virtual void DoRefresh()
		{
			Refresh?.Invoke();
			Dirty = false;
		}

		protected internal ReadOnlyList() { }
		protected internal ReadOnlyList(Action refresh) => Refresh = refresh;

		public int Count
		{
			get
			{
				if (Dirty) DoRefresh();
				return list.Count;
			}
		}
		public virtual bool Contains(T item)
		{
			if (Dirty) DoRefresh();
			return list.Contains(item);
		}
		public void CopyTo(T[] array, int index)
		{
			if (Dirty) DoRefresh();
			list.CopyTo(array, index);
		}

		protected internal virtual void Clear() => list.Clear();
		protected internal virtual bool Add(T item)
		{
			list.Add(item);
			return true;
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public virtual IEnumerator<T> GetEnumerator()
		{
			if (Dirty) DoRefresh();
			return list.GetEnumerator();
		}

		public T this[int index]
		{
			get
			{
				if (Dirty) DoRefresh();
				return list[index];
			}
		}
		public int IndexOf(T item)
		{
			if (Dirty) DoRefresh();
			return list.IndexOf(item);
		}

		bool ICollection<T>.IsReadOnly => true;
		T IList<T>.this[int index]
		{
			get => this[index];
			set => throw new NotImplementedException();
		}
		void ICollection<T>.Add(T item) => throw new NotImplementedException();
		void ICollection<T>.Clear() => throw new NotImplementedException();
		bool ICollection<T>.Remove(T item) => throw new NotImplementedException();
		void IList<T>.Insert(int index, T item) => throw new NotImplementedException();
		void IList<T>.RemoveAt(int index) => throw new NotImplementedException();
	}
	public class PartSet<Part>
		: ReadOnlyList<Part>
		, IPartSet
		where Part : PartBase
	{
		protected Dictionary<global::Part, Part> cache = new Dictionary<global::Part, Part>();
		protected ResourceList resources;
		public ResourceList Resources => resources ?? (resources = new ResourceList(this));

		protected internal PartSet() { }
		protected internal PartSet(Action refresh) : base(refresh) { }

		bool IPartSet.Dirty => Dirty;
		bool ICollection<PartBase>.IsReadOnly => true;
		void IPartSet.SetDirty() => SetDirty();
		event Action IPartSet.Refresh
		{
			add => Refresh += value;
			remove => Refresh -= value;
		}
		IEnumerator<PartBase> IEnumerable<PartBase>.GetEnumerator()
		{
			foreach (var part in this)
				yield return part;
		}
		void ICollection<PartBase>.Add(PartBase item) => throw new NotImplementedException();
		void ICollection<PartBase>.Clear() => throw new NotImplementedException();
		bool ICollection<PartBase>.Remove(PartBase item) => throw new NotImplementedException();
		bool ICollection<PartBase>.Contains(PartBase item)
		{
			if (Dirty) DoRefresh();
			return cache.ContainsKey(item.Native);
		}
		void ICollection<PartBase>.CopyTo(PartBase[] array, int index)
		{
			foreach (var part in this)
				array[index++] = part;
		}

		public override bool Contains(Part item)
		{
			if (Dirty) DoRefresh();
			return cache.ContainsKey(item.Native);
		}

		protected internal override void Clear()
		{
			list.Clear();
			cache.Clear();
		}
		protected internal override bool Add(Part item)
		{
			if (cache.ContainsKey(item.Native))
				return false;
			list.Add(item);
			cache[item.Native] = item;
			return true;
		}

		public Part this[global::Part part]
		{
			get
			{
				if (Dirty) DoRefresh();
				return cache[part];
			}
		}
	}
	public class ShipPartSet : PartSet<PartBase>, IDisposable
	{
		public Ship Ship { get; protected set; }
		public ReadOnlyList<Decoupler> Decouplers { get; protected set; }
		public ReadOnlyList<DockingPort> DockingPorts { get; protected set; }
		public ReadOnlyList<Engine> Engines { get; protected set; }

		protected PartBase root;
		public PartBase Root
		{
			get
			{
				if (Dirty) DoRefresh();
				return root;
			}
		}

		protected Decoupler nextDecoupler;
		public Decoupler NextDecoupler
		{
			get
			{
				if (Dirty) DoRefresh();
				return nextDecoupler;
			}
		}
		public int NextDecoupleStage => NextDecoupler?.Stage ?? -1;
		public int NextDecouplerStage => NextDecoupler?.Stage ?? -1;

		protected internal ShipPartSet(Ship ship)
		{
			Ship = ship;
			Decouplers = new ReadOnlyList<Decoupler>(DoRefresh);
			DockingPorts = new ReadOnlyList<DockingPort>(DoRefresh);
			Engines = new ReadOnlyList<Engine>(DoRefresh);
		}
		protected internal override void SetDirty()
		{
			if (Dirty) return;
			RedOnion.ROS.Value.DebugLog("Ship Parts Dirty");
			GameEvents.onVesselWasModified.Remove(VesselModified);
			base.SetDirty();
			Decouplers.SetDirty();
			DockingPorts.SetDirty();
			Engines.SetDirty();
			Stage.SetDirty();
		}
		void VesselModified(Vessel vessel)
		{
			if (vessel == Ship.Native)
				SetDirty();
		}

		~ShipPartSet() => Dispose(false);
		[Browsable(false), MoonSharpHidden]
		public void Dispose()
		{
			GC.SuppressFinalize(this);
			Dispose(true);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (cache != null)
			{
				GameEvents.onVesselWasModified.Remove(VesselModified);
				list.Clear();
				cache = null;
				Ship = null;
				root = null;
				nextDecoupler = null;
				Decouplers = null;
				DockingPorts = null;
				Engines = null;
			}
		}

		//TODO: reuse wrappers
		protected override void DoRefresh()
		{
			root = null;
			nextDecoupler = null;
			Decouplers.Clear();
			DockingPorts.Clear();
			Engines.Clear();
			Construct(Ship.Native.rootPart, null, null);
			base.DoRefresh();
			Decouplers.Dirty = false;
			DockingPorts.Dirty = false;
			Engines.Dirty = false;
			GameEvents.onVesselWasModified.Add(VesselModified);
			RedOnion.ROS.Value.DebugLog("Ship Parts Refreshed (Parts: {0}, Engines: {1})", list.Count, Engines.Count);
		}
		protected void Construct(Part part, PartBase parent, Decoupler decoupler)
		{
			if (part.State == PartStates.DEAD || part.transform == null)
				return;

			PartBase self = null;
			foreach (var module in part.Modules)
			{
				if (module is IEngineStatus)
				{
					var engine = new Engine(Ship, part, parent, decoupler);
					Engines.Add(engine);
					self = engine;
					break;
				}
				if (module is IStageSeparator)
				{
					var dock = module as ModuleDockingNode;
					if (dock != null)
					{
						var port = new DockingPort(Ship, part, parent, decoupler, dock);
						DockingPorts.Add(port);
						self = port;
						if (!module.StagingEnabled())
							break;
						decoupler = port;
					}
					else
					{
						// ignore anything with staging disabled and continue the search
						// this can e.g. be heat shield or some sensor with integrated decoupler
						if (!module.StagingEnabled())
							continue;
						if (module is global::LaunchClamp)
						{
							var clamp = new LaunchClamp(Ship, part, parent, decoupler);
							self = clamp;
							decoupler = clamp;
						}
						else if (module is ModuleDecouple || module is ModuleAnchoredDecoupler)
						{
							var separator = new Decoupler(Ship, part, parent, decoupler);
							self = separator;
							decoupler = separator;
						}
						else // ModuleServiceModule ?
							continue; // rather continue the search
					}
					Decouplers.Add(decoupler);
					// ignore leftover decouplers
					if (decoupler.Native.inverseStage >= StageManager.CurrentStage)
						break;
					// check if we just created closer decoupler (see StageValues.CreatePartSet)
					if (nextDecoupler == null || decoupler.Native.inverseStage > nextDecoupler.Native.inverseStage)
						nextDecoupler = decoupler;
					break;
				}
				var sensor = module as ModuleEnviroSensor;
				if (sensor != null)
				{
					self = new Sensor(Ship, part, parent, decoupler, sensor);
					break;
				}
			}
			if (self == null)
				self = new PartBase(Ship, part, parent, decoupler);
			if (root == null)
				root = self;
			if (cache == null)
				cache = new Dictionary<Part, PartBase>();
			cache[part] = self;
			list.Add(self);
			foreach (var child in part.children)
				Construct(child, self, decoupler);
		}
	}
}
