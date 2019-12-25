using MunOS.Repl;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static RedOnion.Debugging.QueueLogger;

namespace MunOS
{
	public abstract class MunProcess : ICollection<MunThread>
	{
		public static MunProcess Current => MunThread.Current?.Process;
		public static MunID CurrentID => Current?.ID ?? MunID.Zero;

		public MunID ID { get; internal set; }
		public string Name { get; set; }
		public MunCore Core { get; }

		/// <summary>
		/// Automatically remove the process from the core when it is terminated (last foreground thread is terminated).
		/// Set to false if you want to reuse the process - but you have to remove it yourself.
		/// </summary>
		public bool AutoRemove { get; set; } = true;

		private OutputBuffer _outputBuffer;
		/// <summary>
		/// This is used by REPL and may be null (but used if assigned).
		/// </summary>
		public OutputBuffer OutputBuffer
		{
			get => _outputBuffer;
			set
			{
				if (value == _outputBuffer)
					return;
				var prev = _outputBuffer;
				_outputBuffer = value;
				OnSetOutputBuffer(value, prev);
			}
		}
		/// <summary>
		/// Associated script manager (if any - can be null).
		/// </summary>
		public ScriptManager ScriptManager { get; set; }

		protected MunProcess(MunCore core, string name = null)
		{
			ID = MunID.GetPositive();
			Name = name ?? ID.ToString();
			Core = core;
		}

		protected readonly Dictionary<MunID, MunThread> threads = new Dictionary<MunID, MunThread>();
		public int Count => threads.Count;
		public int ForegroundCount { get; protected internal set; }
		public int BackgroundCount { get; protected internal set; }
		bool ICollection<MunThread>.IsReadOnly => false;

		public IEnumerator<MunThread> GetEnumerator() => threads.Values.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => threads.Values.GetEnumerator();

		// TODO: this is currently only called from MunThread constructor,
		// but since it is public, consider other usage
		public virtual void Add(MunThread thread)
		{
			MunLogger.DebugLog($"Adding thread#{thread?.ID ?? MunID.Zero} into process#{ID}");
			if (thread.Process != null)
				throw new InvalidOperationException(thread.Process == this
					? $"Thread {thread} already is in process {thread.Process}"
					: $"Thread {thread} belongs to process {thread.Process}, cannot add to {this}");
			threads.Add(thread.ID, thread);
			thread.Process = this;
			if (thread.IsBackground)
				++BackgroundCount;
			else ++ForegroundCount;
			Debug.Assert(Count == ForegroundCount + BackgroundCount);
		}
		// TODO: this is currently only called from OnThreadDone,
		// but since it is public, consider other usage
		public virtual void Remove(MunThread thread)
		{
			MunLogger.DebugLog($"Removing thread#{thread?.ID ?? MunID.Zero} from process#{ID}");
			if (thread.Process != this)
				throw new InvalidOperationException($"Thread {thread} does not belong to process {this}");
			threads.Remove(thread.ID);
			thread.Process = null;
			if (thread.IsBackground)
				--BackgroundCount;
			else --ForegroundCount;
			CheckForegroundCount();
		}
		bool ICollection<MunThread>.Remove(MunThread item)
		{
			if (item.Process != this)
				return false;
			Remove(item);
			return true;
		}
		protected internal virtual void OnBackgroundChange(MunThread thread)
		{
			if (thread.IsBackground)
			{
				--ForegroundCount;
				++BackgroundCount;
				CheckForegroundCount();
			}
			else
			{
				++ForegroundCount;
				--BackgroundCount;
			}
		}
		/// <summary>
		/// Terminate all background threads if there is no foreground left.
		/// </summary>
		protected internal virtual void CheckForegroundCount()
		{
			Debug.Assert(Count == ForegroundCount + BackgroundCount);
			if (!AutoRemove)
				return;
			if (BackgroundCount > 0)
				return;
			if (ForegroundCount == 0)
				return;
			Terminate();
		}

		/// <summary>
		/// Every thread notifies the process when it is done executing
		/// (really done executing - finished and not restarted or terminated).
		/// </summary>
		protected internal virtual void OnThreadDone(MunThread thread)
		{
			ThreadDone?.Invoke(thread);
			if (thread.AutoRemove)
				Remove(thread);
		}

		public event Action<MunThread> ThreadDone;

		protected internal virtual void OnError(MunEvent err) { }

		protected virtual void OnSetOutputBuffer(OutputBuffer value, OutputBuffer prev) { }

		/// <summary>
		/// Event invoked on every physics update (Unity FixedUpdate).
		/// </summary>
		public event Action physicsUpdate;

		/// <summary>
		/// To be called on every fixed update.
		/// </summary>
		public virtual void FixedUpdate()
		{
			var physics = physicsUpdate;
			if (physics != null)
			{
				foreach (Action handler in physics.GetInvocationList())
				{
					try
					{
						handler();
					}
					catch (Exception ex)
					{
						// TODO: hook to Core.OnError and similar
						physicsUpdate -= handler;
						MunLogger.DebugLogArray($"Exception in process #{ID} physics update: {ex.Message}");
					}
				}
			}
		}

		/// <summary>
		/// Event invoked on every graphics update (Unity Update).
		/// </summary>
		public event Action graphicsUpdate;

		public virtual void Update()
		{
			var graphics = graphicsUpdate;
			if (graphics != null)
			{
				foreach (Action handler in graphics.GetInvocationList())
				{
					try
					{
						handler();
					}
					catch (Exception ex)
					{
						// TODO: hook to Core.OnError and similar
						physicsUpdate -= handler;
						MunLogger.DebugLog($"Exception in process #{ID} graphics update: {ex.Message}");
					}
				}
			}
		}

		/// <summary>
		/// Process terminated.
		/// Subscribers can use <see cref="ShutdownHook{T}" /> to avoid hard-links.
		/// All subscriptions are removed prior to executing the handlers.
		/// </summary>
		public Action shutdown;

		public virtual void Terminate(bool hard = false)
		{
			// first notify all subscribers that this process is shutting down
			var shutdown = this.shutdown;
			MunLogger.DebugLog($"Process#{ID} terminating. (shutdown: {shutdown?.GetInvocationList().Length ?? 0})");
			if (shutdown != null)
			{
				this.shutdown = null;
				foreach (var fn in shutdown.GetInvocationList())
				{
					try
					{
						fn.DynamicInvoke();
					}
					catch (Exception ex)
					{
						// TODO: hook to Core.OnError and similar
						MunLogger.DebugLog($"Exception in process#{ID} shutdown: {ex.Message}");
					}
				}
			}

			foreach (var thread in threads.Values)
				thread.Terminate(hard);

			MunLogger.DebugLog($"Process#{ID} terminated.");
		}

		void ICollection<MunThread>.Clear() => Terminate();
		bool ICollection<MunThread>.Contains(MunThread thread) => thread.Process == this;
		public void CopyTo(MunThread[] array, int arrayIndex) => threads.Values.CopyTo(array, arrayIndex);

		public override string ToString()
			=> string.IsNullOrWhiteSpace(Name) ? ID.ToString() : Name;

		/// <summary>
		/// Used to subscribe to <see cref="shutdown"/> but avoid direct hard-link
		/// so that the target/subscriber can be garbage-collected.
		/// </summary>
		/// <remarks>
		/// The target/subscriber should dispose this object in its own Dispose() method.
		/// </remarks>
		public class ShutdownHook : ShutdownHook<IDisposable>
		{
			public ShutdownHook(IDisposable target) : base(target) { }
		}
		/// <summary>
		/// Used to subscribe to <see cref="shutdown"/> but avoid direct hard-link
		/// so that the target/subscriber can be garbage-collected.
		/// </summary>
		/// <remarks>
		/// The target/subscriber should dispose this object in its own Dispose() method.
		/// </remarks>
		public class ShutdownHook<T> : IDisposable
			where T : class, IDisposable
		{
			protected WeakReference<T> _target;
			public MunProcess process { get; protected set; }
			public T Target
			{
				get
				{
					T target = null;
					return _target?.TryGetTarget(out target) == true ? target : null;
				}
			}

			public ShutdownHook(T target)
			{
				MunLogger.DebugLog($"Creating ShutdownHook for process#{CurrentID} in thread#{MunThread.CurrentID}.");
				_target = new WeakReference<T>(target);
				process = Current;
				process.shutdown += Shutdown;
			}
			~ShutdownHook() => Dispose(false);
			public void Dispose()
			{
				GC.SuppressFinalize(this);
				Dispose(true);
			}
			protected virtual void Dispose(bool disposing)
			{
				if (process == null)
					return;
				process.shutdown -= Shutdown;
				process = null;
				_target = null;

			}
			protected virtual void Shutdown()
			{
				var target = Target;
				MunLogger.DebugLog($@"ShutdownHook invoked for process ID#{process.ID}, {(
					target == null ? "target is null" : "disposing target")}.");
				target?.Dispose();
				Dispose();
			}
		}
	}
}