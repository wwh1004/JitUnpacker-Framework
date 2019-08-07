using System;
using System.Cli;
using System.IO;
using JitTools.Runtime;

namespace JitTools {
	public sealed class JitUnpackerSettings {
		private string _assemblyPath;
		private JitHookType _hookType;
		private bool _dumpBeforeStaticConstructor;
		private bool _preserveAll;
		private bool _preserveRuntime;
		private bool _preserveTokens;
		private bool _keepMaxStacks;
		private bool _useNativeWriter;

		[Argument("-f", IsRequired = true, Type = "FILE", Description = "Assembly path")]
		internal string AssemblyPathCliSetter {
			set => AssemblyPath = value;
		}

		[Argument("-hook-type", IsRequired = false, DefaultValue = "Inline", Type = "STR", Description = "JIT hook type")]
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

		[Argument("--dump-before-cctor", Description = "Dump module beform run static constructor")]
		internal bool DumpBeforeStaticConstructorCliSetter {
			set => _dumpBeforeStaticConstructor = value;
		}

		[Argument("--preserve-all", Description = "Preserve all(--preserve-tokens --preserve-tokens --keep-max-stacks --use-native-writer)")]
		internal bool PreserveAllCliSetter {
			set => _preserveAll = value;
		}

		[Argument("--preserve-runtime", Description = "Preserve packer runtime")]
		internal bool PreserveRuntimeCliSetter {
			set => _preserveRuntime = value;
		}

		[Argument("--preserve-tokens", Description = "Preserve original tokens")]
		internal bool PreserveTokensCliSetter {
			set => PreserveTokens = value;
		}

		[Argument("--keep-max-stacks", Description = "Keep old max-stacks")]
		internal bool KeepMaxStacksCliSetter {
			set => KeepMaxStacks = value;
		}

		[Argument("--use-native-writer", Description = "Use dnlib.DotNet.Writer.NativeModuleWriter")]
		internal bool UseNativeWriterCliSetter {
			set => UseNativeWriter = value;
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
			get => _preserveAll || _preserveRuntime;
			set => _preserveRuntime = value;
		}

		public bool PreserveTokens {
			get => _preserveAll || _preserveTokens;
			set => _preserveTokens = value;
		}

		public bool KeepMaxStacks {
			get => _preserveAll || _keepMaxStacks;
			set => _keepMaxStacks = value;
		}

		public bool UseNativeWriter {
			get => _preserveAll || _useNativeWriter;
			set => _useNativeWriter = value;
		}
	}
}
