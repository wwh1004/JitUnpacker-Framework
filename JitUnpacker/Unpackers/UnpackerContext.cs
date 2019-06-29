using System;
using System.Reflection;
using dnlib.DotNet;

namespace JitTools.Unpackers {
	internal sealed unsafe class UnpackerContext {
		private readonly Module _module;
		private readonly ModuleDefMD _moduleDef;
		private readonly void* _moduleHandle;
		private readonly void*[] _methodHandles;
		private readonly JitUnpackerSettings _settings;
		private ModuleDefMD _dumpedModuleDef;

		public Module Module => _module;

		public ModuleDefMD ModuleDef => _moduleDef;

		public void* ModuleHandle => _moduleHandle;

		public void*[] MethodHandles => _methodHandles;

		public JitUnpackerSettings Settings => _settings;

		public ModuleDefMD DumpedModuleDef {
			get => _dumpedModuleDef;
			set => _dumpedModuleDef = value;
		}

		/// <summary>
		/// 构造器
		/// </summary>
		/// <param name="module">模块</param>
		/// <param name="moduleDef">模块</param>
		/// <param name="moduleHandle">要脱壳的程序集的.NET模块句柄</param>
		/// <param name="methodHandles">要脱壳的程序集的所有方法句柄</param>
		/// <param name="settings"></param>
		public UnpackerContext(Module module, ModuleDefMD moduleDef, void* moduleHandle, void*[] methodHandles, JitUnpackerSettings settings) {
			if (module == null)
				throw new ArgumentNullException(nameof(module));
			if (moduleDef == null)
				throw new ArgumentNullException(nameof(moduleDef));
			if (moduleHandle == null)
				throw new ArgumentNullException(nameof(moduleHandle));
			if (methodHandles == null)
				throw new ArgumentNullException(nameof(methodHandles));
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));

			_module = module;
			_moduleDef = moduleDef;
			_moduleHandle = moduleHandle;
			_methodHandles = methodHandles;
			_settings = settings;
		}
	}
}
