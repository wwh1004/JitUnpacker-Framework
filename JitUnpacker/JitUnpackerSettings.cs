using System;
using System.Cli;
using System.IO;
using JitTools.Runtime;

namespace JitTools {
	public sealed class JitUnpackerSettings {
		private string _assemblyPath;
		private JitHookType _hookType;
		private bool _dumpBeforeStaticConstructor;
		private bool _preserveRuntime;
		private bool _preserveTokens;
		private bool _keepMaxStacks;

		[Argument("-f", IsRequired = true, Type = "FILE", Description = "Assembly path")]
		internal string AssemblyPathCliSetter {
			set => AssemblyPath = value;
		}

		[Argument("-hook-type", IsRequired = false, DefaultValue = "Inline", Type = "STR", Description = "Jit hook type")]
		internal string HookTypeCliSetter {
			set {
				switch (value.ToUpperInvariant()) {
				case "INLINE":
					_hookType = JitHookType.Inline;
					break;
				case "VTABLE":
					_hookType = JitHookType.VTable;
					break;
				case "INT3":
					_hookType = JitHookType.Int3;
					break;
				case "HARDWARE":
					_hookType = JitHookType.Hardware;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(value));
				}
			}
		}

		[Argument("--dump-before-cctor", Description = "If dump module beform run static constructor")]
		internal bool DumpBeforeStaticConstructorCliSetter {
			set => _dumpBeforeStaticConstructor = value;
		}

		[Argument("--preserve-runtime", Description = "If preserve packer runtime")]
		internal bool PreserveRuntimeCliSetter {
			set => _preserveRuntime = value;
		}

		[Argument("--preserve-tokens", Description = "If preserve original tokens")]
		internal bool PreserveTokensCliSetter {
			set => PreserveTokens = value;
		}

		[Argument("--keep-max-stacks", Description = "If keep old max-stacks")]
		internal bool KeepMaxStacksCliSetter {
			set => KeepMaxStacks = value;
		}

		public string AssemblyPath {
			get => _assemblyPath;
			set {
				if (string.IsNullOrEmpty(value))
					throw new ArgumentNullException(nameof(value));
				if (!File.Exists(value))
					throw new FileNotFoundException($"{value} does NOT exists");

				_assemblyPath = Path.GetFullPath(value);
			}
		}

		public JitHookType HookType {
			get => _hookType;
			set => _hookType = value;
		}

		public bool DumpBeforeStaticConstructor {
			get => _dumpBeforeStaticConstructor;
			set => _dumpBeforeStaticConstructor = value;
		}

		public bool PreserveRuntime {
			get => _preserveRuntime;
			set => _preserveRuntime = value;
		}

		public bool PreserveTokens {
			get => _preserveTokens;
			set => _preserveTokens = value;
		}

		public bool KeepMaxStacks {
			get => _keepMaxStacks;
			set => _keepMaxStacks = value;
		}
	}
}
