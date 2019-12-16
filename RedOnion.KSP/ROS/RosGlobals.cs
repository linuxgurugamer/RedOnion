using MunOS.Core;
using MunOS.ProcessLayer;
using RedOnion.KSP.API;
using RedOnion.ROS;
using RedOnion.ROS.Objects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedOnion.KSP.ROS
{
	public class RosGlobals : RedOnion.ROS.Objects.Globals
	{
		public override void Fill()
		{
			if (readOnlyTop > 0)
				return;
			base.Fill();
			System.Add(typeof(PID));

			var process = ((RosProcessor)Processor).Process;
			System.Add("update", new Event(ExecPriority.REALTIME));
			System.Add("idle", new Event(ExecPriority.IDLE));
			System.Add("once", new Event(ExecPriority.ONESHOT));
			System.Lock();
		}

		class Event : ICallable
		{
			public readonly ExecPriority priority;
			public Event(ExecPriority priority)
				=> this.priority = priority;

			bool ICallable.Call(ref Value result, object self, Arguments args, bool create)
			{
				if (args.Length != 1)
					return false;
				var it = args[0];
				if (!it.IsFunction)
					return false;
				var process = MunThread.ExecutingThread?.parentProcess as RosProcess;
				var thread = new RosThread(priority, it.obj as Function, process);
				process.EnqueueThread(priority, thread);
				result = new Value(new Subscription(thread));
				return true;
			}
		}

		[Description("The auto-remove subscription object returned by call to `system.update` or `idle` (or `once`).")]
		public sealed class Subscription : IDisposable, IEquatable<Subscription>, IEquatable<Value>
		{
			RosThread thread;
			bool autoRemove;
			internal Subscription(RosThread thread, bool autoRemove = true)
			{
				this.thread = thread;
				this.autoRemove = autoRemove;
			}

			[Browsable(false)]
			public bool Equals(Subscription it)
				=> ReferenceEquals(this, it);
			[Browsable(false)]
			public bool Equals(Value value)
				=> value.obj is RosThread t && ReferenceEquals(this, t);
			[Browsable(false)]
			public override bool Equals(object o)
				=> o is Value v ? Equals(v) : ReferenceEquals(this, o);
			[Browsable(false)]
			public override int GetHashCode()
				=> thread.GetHashCode();

			[Description("Remove this subscription from its list. (Ignored if already removed.)")]
			public void Remove()
			{
				thread.Terminate();
				autoRemove = false;
				GC.SuppressFinalize(this);
			}
			void IDisposable.Dispose()
				=> Remove();
			~Subscription()
			{
				if (autoRemove)
					thread.TerminateAsync();
			}
		}

		class ReflectedGlobals : Reflected
		{
			public ReflectedGlobals() : base(typeof(API.Globals)) { }
			public int Count => prop.Count;
			public ref Prop this[int idx] => ref prop.items[idx];
			public IEnumerator<string> GetEnumerator()
			{
				for (int i = 0; i < prop.size; i++)
					yield return prop.items[i].name;
			}
		}
		const int mark = 0x7F000000;
		static ReflectedGlobals reflected = new ReflectedGlobals();

		public override int Find(string name)
		{
			int at = reflected.Find(null, name, false);
			if (at >= 0) return at + mark;
			return base.Find(name);
		}
		public override bool Get(ref Value self, int at)
		{
			if (at < mark)
				return base.Get(ref self, at);
			if ((at -= mark) >= reflected.Count)
				return false;
			ref var member = ref reflected[at];
			if (member.read == null)
				return false;
			self = member.read(self.obj);
			return true;
		}
		public override bool Set(ref Value self, int at, OpCode op, ref Value value)
		{
			if (at < mark)
				return base.Set(ref self, at, op, ref value);
			if ((at -= mark) >= reflected.Count)
				return false;
			ref var member = ref reflected[at];
			if (member.write == null)
				return false;
			if (op != OpCode.Assign)
				return false;
			member.write(self.obj, value);
			return true;
		}
		public override IEnumerable<string> EnumerateProperties(object self)
		{
			var seen = new HashSet<string>();
			foreach (var member in reflected)
			{
				seen.Add(member);
				yield return member;
			}
			foreach (var name in EnumerateProperties(self, seen))
				yield return name;
		}
	}
}
