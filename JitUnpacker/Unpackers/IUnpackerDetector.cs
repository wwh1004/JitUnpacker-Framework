using System;
using System.Collections.Generic;
using dnlib.DotNet;

namespace JitTools.Unpackers {
	internal unsafe interface IUnpackerDetector {
		/// <summary>
		/// 判断是否为当前Unpacker可以处理的壳
		/// </summary>
		/// <param name="moduleDef"></param>
		/// <returns></returns>
		bool Detect(ModuleDefMD moduleDef);

		/// <summary>
		/// 创建脱壳机
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		IUnpacker CreateUnpacker(UnpackerContext context);
	}

	internal static class LoadedUnpackerDetectors {
		private static readonly IUnpackerDetector[] _allWithoutUnknown;
		private static readonly IUnpackerDetector _unknown;

		public static IUnpackerDetector[] AllWithoutUnknown => _allWithoutUnknown;

		public static IUnpackerDetector Unknown => _unknown;

		static LoadedUnpackerDetectors() {
			Type targetType;
			List<IUnpackerDetector> detectors;

			targetType = typeof(IUnpackerDetector);
			detectors = new List<IUnpackerDetector>();
			foreach (Type type in typeof(IUnpackerDetector).Module.GetTypes())
				foreach (Type interfaceType in type.GetInterfaces())
					if (interfaceType.IsAssignableFrom(targetType)) {
						IUnpackerDetector detector;

						detector = (IUnpackerDetector)Activator.CreateInstance(type);
						if (detector is Unknown.UnpackerDetector)
							_unknown = detector;
						else
							detectors.Add(detector);
					}
			_allWithoutUnknown = detectors.ToArray();
		}

		public static IUnpackerDetector Detect(ModuleDefMD moduleDef) {
			foreach (IUnpackerDetector detector in _allWithoutUnknown)
				if (detector.Detect(moduleDef))
					return detector;
			return _unknown;
		}
	}
}
