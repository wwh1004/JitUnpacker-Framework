using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using static JitTools.NativeMethods;

namespace JitTools.Runtime {
	internal static unsafe class RuntimeConstants {
#pragma warning disable CS0649
		public static readonly uint METHODDESC_RESET_RVA;
		public static readonly uint METHODDESC_DOPRESTUB_RVA;
		public static readonly uint METHODDESC_GETWRAPPEDMETHODDESC_RVA;
		public static readonly uint CEEINFO_CANINLINE_RVA;
		public static readonly uint METHODDESC_DOPRESTUB_CALL_CONTAINSGENERICVARIABLES_RVA;
		public static readonly uint METHODDESC_DOPRESTUB_CALL_ISCLASSCONSTRUCTORTRIGGEREDVIAPRESTUB_RVA;
		public static readonly uint METHODDESC_DOPRESTUB_CALL_CHECKRUNCLASSINITTHROWING_RVA;
		public static readonly uint CALL_COMPCOMPILE_RVA;
		public static readonly uint JITNATIVECODE_RVA;
#pragma warning restore CS0649

		static RuntimeConstants() {
			string configName;
			string[][] configs;

			configName = "JitUnpacker.RuntimeFunctions";
			if (RuntimeEnvironment.Is32Bit)
				configName += RuntimeEnvironment.IsClr2x ? ".CLR20.x86" : ".CLR40.x86";
			else
				configName += RuntimeEnvironment.IsClr2x ? ".CLR20.x64" : ".CLR40.x64";
			configName += ".config";
			configs = File.ReadAllLines(configName).Select(line => line.Split('=')).ToArray();
			CheckVersions(configs);
			foreach (FieldInfo fieldInfo in typeof(RuntimeConstants).GetFields(BindingFlags.Public | BindingFlags.Static)) {
				string key;
				string[] config;

				key = fieldInfo.Name;
				config = configs.First(t => t[0] == key);
				fieldInfo.SetValue(null, GetValue(config[1]));
			}
		}

		private static uint GetValue(string value) {
			return uint.Parse(value, NumberStyles.HexNumber);
		}

		private static void CheckVersions(string[][] configs) {
			CheckVersion(configs, "CLR_VERSION", RuntimeEnvironment.ClrModuleHandle);
			CheckVersion(configs, "JIT_VERSION", RuntimeEnvironment.JitModuleHandle);
		}

		private static void CheckVersion(string[][] configs, string configKey, void* moduleHandle) {
			StringBuilder buffer;
			string filePath;
			FileVersionInfo versionInfo;
			Version version;

			buffer = new StringBuilder((int)MAX_PATH);
			GetModuleFileName(moduleHandle, buffer, MAX_PATH);
			filePath = buffer.ToString();
			versionInfo = FileVersionInfo.GetVersionInfo(filePath);
			version = new Version(versionInfo.FileMajorPart, versionInfo.FileMinorPart, versionInfo.FileBuildPart, versionInfo.FilePrivatePart);
			if (configs.First(t => t[0] == configKey)[1] != version.ToString())
				throw new NotSupportedException("You should run RuntimeFunctionConfigGenerator to update JitUnpacker.RuntimeFunctions.*.config");
		}
	}
}
