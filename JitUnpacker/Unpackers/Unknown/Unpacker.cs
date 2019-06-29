using System;

namespace JitTools.Unpackers.Unknown {
	internal sealed unsafe class Unpacker : IUnpacker {
		private readonly UnpackerContext _context;
		private readonly MethodDumper _methodDumper;

		public string Name => "Unknown";

		public string Id => "un";

		public UnpackerContext Context => _context;

		public IMethodDumper MethodDumper => _methodDumper;

		public Unpacker(UnpackerContext context) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			_context = context;
			_methodDumper = new MethodDumper(context);
		}

		public void PreInitialize() {
		}

		public void PostInitialize() {
		}

		public bool NeedDecryptMethod(int index) {
			return _context.ModuleDef.ResolveMethod((uint)index + 1).HasBody;
		}

		public void CallJit(int index) {
			UnpackerUtils.CallJit(_context.MethodHandles[index]);
		}

		public void RemoveRuntime() {
		}
	}
}
