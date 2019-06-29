using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using dnlib.PE;
using Tool.Interface;
using static JitTools.NativeMethods;

namespace JitTools {
	public sealed unsafe class RuntimeFunctionConfigGenerator : ITool<RuntimeFunctionConfigGeneratorSettings> {
		private RuntimeFunctionConfigGeneratorSettings _settings;

		public string Title => ConsoleTitleUtils.GetTitle();

		public void Execute(RuntimeFunctionConfigGeneratorSettings settings) {
			if (!RuntimeEnvironment.Is32Bit)
				throw new NotSupportedException();

			_settings = settings;
			PdbInitialize();
			GenerateForJitUnpacker();
		}

		private void GenerateForJitUnpacker() {
			string configName;
			StringBuilder config;
			StringBuilder buffer;
			string clrModulePath;
			string jitModulePath;
			byte* address;

			configName = "JitUnpacker.RuntimeFunctions";
			if (RuntimeEnvironment.Is32Bit)
				configName += RuntimeEnvironment.IsClr2x ? ".CLR20.x86" : ".CLR40.x86";
			else
				configName += RuntimeEnvironment.IsClr2x ? ".CLR20.x64" : ".CLR40.x64";
			configName += ".config";
			config = new StringBuilder();

			buffer = new StringBuilder((int)MAX_PATH);
			GetModuleFileName(RuntimeEnvironment.ClrModuleHandle, buffer, MAX_PATH);
			clrModulePath = buffer.ToString();
			GetModuleFileName(RuntimeEnvironment.JitModuleHandle, buffer, MAX_PATH);
			jitModulePath = buffer.ToString();

			WriteConfig(config, "CLR_VERSION", GetFileVersion(clrModulePath).ToString());
			WriteConfig(config, "JIT_VERSION", GetFileVersion(jitModulePath).ToString());

			WriteFunctionRva(config, "?Reset@MethodDesc@@QAEXXZ", "METHODDESC_RESET_RVA");
			WriteFunctionRva(config, "?DoPrestub@MethodDesc@@QAEKPAVMethodTable@@@Z", "METHODDESC_DOPRESTUB_RVA");
			WriteFunctionRva(config, "?GetWrappedMethodDesc@MethodDesc@@QAEPAV1@XZ", "METHODDESC_GETWRAPPEDMETHODDESC_RVA");

			if (RuntimeEnvironment.IsClr45x)
				WriteFunctionRva(config, "?canInline@CEEInfo@@UAE?AW4CorInfoInline@@PAUCORINFO_METHOD_STRUCT_@@0PAK@Z", "CEEINFO_CANINLINE_RVA");
			else
				WriteFunctionRva(config, "?canInline@CEEInfo@@UAG?AW4CorInfoInline@@PAUCORINFO_METHOD_STRUCT_@@0PAK@Z", "CEEINFO_CANINLINE_RVA");

			if (RuntimeEnvironment.IsClr45x) {
				address = GetFirstCallAddress("?DoPrestub@MethodDesc@@QAEKPAVMethodTable@@@Z", "?ContainsGenericVariables@MethodDesc@@QAEHXZ");
				address = GetFirstTestEaxEaxAddress(address);
				WriteConfig(config, "METHODDESC_DOPRESTUB_CALL_CONTAINSGENERICVARIABLES_RVA", (uint)(address - (byte*)RuntimeEnvironment.ClrModuleHandle));
				address = GetFirstCallAddress("?DoPrestub@MethodDesc@@QAEKPAVMethodTable@@@Z", "?IsClassConstructorTriggeredViaPrestub@MethodDesc@@QAEHXZ");
				address = GetFirstTestEaxEaxAddress(address);
				WriteConfig(config, "METHODDESC_DOPRESTUB_CALL_ISCLASSCONSTRUCTORTRIGGEREDVIAPRESTUB_RVA", (uint)(address - (byte*)RuntimeEnvironment.ClrModuleHandle));
				WriteConfig(config, "METHODDESC_DOPRESTUB_CALL_CHECKRUNCLASSINITTHROWING_RVA", 0);
			}
			else {
				WriteConfig(config, "METHODDESC_DOPRESTUB_CALL_CONTAINSGENERICVARIABLES_RVA", 0);
				WriteConfig(config, "METHODDESC_DOPRESTUB_CALL_ISCLASSCONSTRUCTORTRIGGEREDVIAPRESTUB_RVA", 0);
				address = GetFirstCallAddress("?DoPrestub@MethodDesc@@QAEKPAVMethodTable@@@Z", "?CheckRunClassInitThrowing@MethodTable@@QAEXXZ");
				address -= 3;
				WriteConfig(config, "METHODDESC_DOPRESTUB_CALL_CHECKRUNCLASSINITTHROWING_RVA", (uint)(address - (byte*)RuntimeEnvironment.ClrModuleHandle));
			}

			if (RuntimeEnvironment.IsClr45x)
				address = GetFirstCallAddress("jitNativeCode", "?compCompile@Compiler@@QAIHPAUCORINFO_METHOD_STRUCT_@@PAUCORINFO_MODULE_STRUCT_@@PAVICorJitInfo@@PAUCORINFO_METHOD_INFO@@PAPAXPAKI@Z");
			else
				address = GetFirstCallAddress("?jitNativeCode@@YIHPAUCORINFO_METHOD_STRUCT_@@PAUCORINFO_MODULE_STRUCT_@@PAVICorJitInfo@@PAUCORINFO_METHOD_INFO@@PAPAXPAKIPAX@Z", "?compCompile@Compiler@@QAIHPAUCORINFO_METHOD_STRUCT_@@PAUCORINFO_MODULE_STRUCT_@@PAVICorJitInfo@@PAUCORINFO_METHOD_INFO@@PAPAXPAKI@Z");
			WriteConfig(config, "CALL_COMPCOMPILE_RVA", (uint)(address - (byte*)RuntimeEnvironment.JitModuleHandle));

			if (RuntimeEnvironment.IsClr45x)
				WriteFunctionRva(config, "jitNativeCode", "JITNATIVECODE_RVA");
			else
				WriteFunctionRva(config, "?jitNativeCode@@YIHPAUCORINFO_METHOD_STRUCT_@@PAUCORINFO_MODULE_STRUCT_@@PAVICorJitInfo@@PAUCORINFO_METHOD_INFO@@PAPAXPAKIPAX@Z", "JITNATIVECODE_RVA");

			File.WriteAllText(configName, config.ToString());
			Console.WriteLine(config.ToString());
			Console.WriteLine("Saving: " + Path.GetFullPath(configName));
			Console.WriteLine();
		}

		private static Version GetFileVersion(string filePath) {
			FileVersionInfo versionInfo;

			versionInfo = FileVersionInfo.GetVersionInfo(filePath);
			return new Version(versionInfo.FileMajorPart, versionInfo.FileMinorPart, versionInfo.FileBuildPart, versionInfo.FilePrivatePart);
		}

		private void PdbInitialize() {
			void* processHandle;

			processHandle = GetCurrentProcess();
			SymSetOptions(SYMOPT_DEFERRED_LOADS);
			SymInitialize(processHandle, _settings.SymbolsDirectory, false);
			SymLoadModuleEx(processHandle, null, RuntimeEnvironment.ClrModuleName, null, (ulong)RuntimeEnvironment.ClrModuleHandle, 0, null, 0);
			SymLoadModuleEx(processHandle, null, RuntimeEnvironment.JitModuleName, null, (ulong)RuntimeEnvironment.JitModuleHandle, 0, null, 0);
		}

		private static void WriteFunctionRva(StringBuilder config, string functionName, string escapedFunctionName) {
			uint rva;

			rva = GetFunctionRva(functionName);
			WriteConfig(config, escapedFunctionName, rva);
		}

		private static uint GetFunctionRva(string functionName) {
			SYMBOL_INFO symbolInfo;
			uint rva;

			if (!SymFromName(GetCurrentProcess(), functionName, &symbolInfo)) {
				Console.WriteLine($"error on \"{functionName}\"");
				throw new ApplicationException();
			}
			rva = (uint)(symbolInfo.Address - symbolInfo.ModBase);
			return rva;
		}

		private static byte* GetFunctionAddress(string functionName) {
			SYMBOL_INFO symbolInfo;

			if (!SymFromName(GetCurrentProcess(), functionName, &symbolInfo)) {
				Console.WriteLine($"error on \"{functionName}\"");
				throw new ApplicationException();
			}
			return (byte*)symbolInfo.Address;
		}

		private static byte* GetFirstCallAddress(string functionName, string callTargetName) {
			Ldasm ldasm;
			byte* p;
			void* pTarget;

			ldasm = new Ldasm();
			p = GetFunctionAddress(functionName);
			pTarget = GetFunctionAddress(callTargetName);
			while (true)
				if (*p == 0xE8 && p + *(int*)(p + 1) + 5 == pTarget)
					return p;
				else
					p += ldasm.Disassemble(p, !RuntimeEnvironment.Is32Bit);
		}

		private static byte* GetFirstTestEaxEaxAddress(byte* p) {
			Ldasm ldasm;

			ldasm = new Ldasm();
			while (true)
				if (*p == 0x85 && *(p + 1) == 0xC0)
					return p;
				else
					p += ldasm.Disassemble(p, !RuntimeEnvironment.Is32Bit);
		}

		private static void WriteConfig(StringBuilder config, string name, uint value) {
			WriteConfig(config, name, value.ToString("X"));
		}

		private static void WriteConfig(StringBuilder config, string name, string value) {
			config.AppendLine(name + "=" + value);
		}
	}
}
