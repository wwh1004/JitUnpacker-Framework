using dnlib.DotNet;

namespace JitTools.Unpackers.Unknown {
	internal sealed class UnpackerDetector : IUnpackerDetector {
		public bool Detect(ModuleDefMD moduleDef) {
			return true;
		}

		public IUnpacker CreateUnpacker(UnpackerContext context) {
			return new Unpacker(context);
		}
	}
}
