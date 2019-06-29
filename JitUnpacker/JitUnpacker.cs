using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using JitTools.Runtime;
using JitTools.Unpackers;
using Tool.Interface;
using static JitTools.NativeMethods;
using RuntimeEnvironment = JitTools.Runtime.RuntimeEnvironment;
using System.Linq;
using System.Runtime.CompilerServices;

namespace JitTools {
	public sealed unsafe partial class JitUnpacker : ITool<JitUnpackerSettings> {
		private static IUnpacker _unpacker;

		public string Title => ConsoleTitleUtils.GetTitle();

		public void Execute(JitUnpackerSettings settings) {
			Module module;
			void* moduleHandle;
			ModuleDefMD moduleDef;
			void*[] methodHandles;

			PrepareAllMethods();
			// 防止陷入编译死循环
			module = Assembly.LoadFile(settings.AssemblyPath).ManifestModule;
			if (RuntimeEnvironment.IsClr4x)
				moduleHandle = (void*)(IntPtr)module.GetType().GetField("m_pData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(module);
			else
				moduleHandle = (void*)(IntPtr)typeof(ModuleHandle).GetField("m_ptr", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(module.ModuleHandle);
			moduleDef = ModuleDefMD.Load(settings.AssemblyPath);
			methodHandles = LoadMethodHandles(module, moduleDef);
			_unpacker = LoadedUnpackerDetectors.Detect(moduleDef).CreateUnpacker(new UnpackerContext(module, moduleDef, moduleHandle, methodHandles, settings));
			Logger.Instance.LogInfo($"Detected {_unpacker.Name} Obfuscator ({Path.GetFullPath(settings.AssemblyPath)})");
			Console.CursorVisible = false;
			ExecuteImpl();
			Console.CursorVisible = true;
			SaveAs(PathInsertPostfix(settings.AssemblyPath, ".jupk"));
			_unpacker.Context.ModuleDef.Dispose();
			_unpacker.Context.DumpedModuleDef.Dispose();
			Logger.Instance.LogInfo("Finished");
			Logger.Instance.LogNewLine();
		}

		[HandleProcessCorruptedStateExceptions]
		private static void ExecuteImpl() {
			ProgressBar progressBar;
			MethodDef cctor;

			progressBar = new ProgressBar(_unpacker.Context.MethodHandles.Length);
			RuntimePatcher.PatchAll();
			// Patch CLR和JIT
			_unpacker.MethodDumper.Hook();
			// 先Hook，再进行其它步骤，防止Hook被绕过
			_unpacker.PreInitialize();
			cctor = FindStaticConstructor(_unpacker.Context.ModuleDef);
			if (_unpacker.Context.Settings.DumpBeforeStaticConstructor) {
				// 要脱壳的文件DUMP时机太后
				// 比如DNG加壳的程序套了层TMD，程序完全跑起来之后才DUMP的主程序
				// 这时要先读取元数据流
				_unpacker.Context.DumpedModuleDef = ModuleDefMD.Load(DumpModule());
				// Dump元数据流和.NET资源
			}
			if (cctor == null)
				Logger.Instance.LogError("WARNING: Not fount any static constructor!");
			else {
				// 先运行静态构造器初始化运行时（如果不存在，就是其它静态构造器）再Dump才能得到正确数据（比如元数据流和.NET资源）
				_unpacker.Context.Module.ResolveMethod(cctor.MDToken.ToInt32()).Invoke(null, null);
				_unpacker.PostInitialize();
			}
			if (!_unpacker.Context.Settings.DumpBeforeStaticConstructor) {
				_unpacker.Context.DumpedModuleDef = ModuleDefMD.Load(DumpModule());
				// Dump元数据流和.NET资源
			}
			for (int i = 0; i < _unpacker.Context.MethodHandles.Length; i++) {
				uint oldDumpCount;

				progressBar.Current = i + 1;
				if (_unpacker.Context.MethodHandles[i] == null)
					continue;
				oldDumpCount = _unpacker.MethodDumper.DumpCount;
				if (!_unpacker.NeedDecryptMethod(i))
					continue;
				try {
					_unpacker.MethodDumper.SetTargetMethod(i);
					_unpacker.CallJit(i);
					_unpacker.MethodDumper.SetIdle();
					if (_unpacker.MethodDumper.DumpCount != oldDumpCount + 1)
						throw new Exception("Failed to dump current method.");
				}
				catch (Exception ex) {
					_unpacker.MethodDumper.SetIdle();
					Logger.Instance.LogError("Exception: 0x" + (0x06000001 + i).ToString("X8") + " " + _unpacker.Context.ModuleDef.ResolveMethod((uint)i + 1).ToString());
					Logger.Instance.LogException(ex);
					Logger.Instance.LogNewLine();
					Logger.Instance.LogNewLine();
				}
			}
			_unpacker.MethodDumper.Unhook();
			RuntimePatcher.RestoreAll();
			if (!_unpacker.Context.Settings.PreserveRuntime) {
				Logger.Instance.LogInfo("Removing runtime type");
				_unpacker.RemoveRuntime();
			}
			FillNullSignatures(_unpacker.Context.DumpedModuleDef);
		}

		private static void*[] LoadMethodHandles(Module module, ModuleDefMD moduleDef) {
			void*[] methodHandles;
			ModuleHandle moduleHandle;

			methodHandles = new void*[moduleDef.TablesStream.MethodTable.Rows];
			moduleHandle = module.ModuleHandle;
			for (int i = 0; i < methodHandles.Length; i++) {
				MethodDef methodDef;

				methodDef = moduleDef.ResolveMethod((uint)i + 1);
				if (!methodDef.HasBody)
					continue;
				methodHandles[i] = (void*)moduleHandle.ResolveMethodHandle(0x06000001 + i).Value;
			}
			return methodHandles;
		}

		private static MethodDef FindStaticConstructor(ModuleDefMD moduleDef) {
			MethodDef cctor;

			cctor = moduleDef.GlobalType.FindStaticConstructor();
			if (cctor != null)
				return cctor;
			foreach (TypeDef typeDef in moduleDef.GetTypes()) {
				IList<Instruction> instructions;

				cctor = typeDef.FindStaticConstructor();
				if (cctor == null)
					continue;
				instructions = cctor.Body.Instructions;
				if (instructions.Count == 2 && instructions[0].OpCode.Code == Code.Call && (instructions[0].Operand is MethodDef))
					// 只有一个call和ret，否则可能带有其它IL导致执行出错
					break;
			}
			return cctor;
		}

		private static byte[] DumpModule() {
			byte* imageBase;
			byte* p;
			ushort sectionCount;
			IMAGE_SECTION_HEADER* pSectionHeaders;
			uint imageSize;
			byte[] peImage;

			imageBase = (byte*)Marshal.GetHINSTANCE(_unpacker.Context.Module);
			p = imageBase;
			p += 0x3C;
			p = imageBase + *(uint*)p;
			p += 0x6;
			sectionCount = *(ushort*)p;
			p += 0xE;
			p = p + *(ushort*)p + 4;
			pSectionHeaders = (IMAGE_SECTION_HEADER*)p;
			imageSize = 0;
			for (int i = 0; i < sectionCount; i++)
				if (pSectionHeaders[i].PointerToRawData >= imageSize)
					imageSize = pSectionHeaders[i].PointerToRawData + pSectionHeaders[i].SizeOfRawData;
			peImage = new byte[imageSize];
			Marshal.Copy((IntPtr)imageBase, peImage, 0, (int)(p - imageBase) + (sizeof(IMAGE_SECTION_HEADER) * sectionCount));
			for (int i = 0; i < sectionCount; i++)
				Marshal.Copy((IntPtr)(imageBase + pSectionHeaders[i].VirtualAddress), peImage, (int)pSectionHeaders[i].PointerToRawData, (int)pSectionHeaders[i].VirtualSize);
			return peImage;
		}

		private static void FillNullSignatures(ModuleDefMD moduleDef) {
			uint rows;

			rows = moduleDef.TablesStream.StandAloneSigTable.Rows;
			for (uint rid = 1; rid <= rows; rid++) {
				StandAloneSig standAloneSig;

				standAloneSig = moduleDef.ResolveStandAloneSig(rid);
				if (standAloneSig.Signature == null)
					standAloneSig.Signature = new LocalSig(moduleDef.CorLibTypes.Int32);
			}
		}

		private static string PathInsertPostfix(string path, string postfix) {
			return Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + postfix + Path.GetExtension(path));
		}

		private static void SaveAs(string filePath) {
			Logger.Instance.LogInfo(_unpacker.MethodDumper.DumpCount.ToString() + " methods are decrypted");
			if (_unpacker.MethodDumper.DumpCount != 0) {
				ModuleWriterOptions moduleWriterOptions;

				moduleWriterOptions = new ModuleWriterOptions(_unpacker.Context.DumpedModuleDef);
				if (_unpacker.Context.Settings.PreserveTokens)
					moduleWriterOptions.MetadataOptions.Flags |= MetadataFlags.PreserveRids | MetadataFlags.PreserveUSOffsets | MetadataFlags.PreserveBlobOffsets | MetadataFlags.PreserveExtraSignatureData;
				if (_unpacker.Context.Settings.KeepMaxStacks)
					moduleWriterOptions.MetadataOptions.Flags |= MetadataFlags.KeepOldMaxStack;
				moduleWriterOptions.Logger = DnlibLogger.Instance;
				Logger.Instance.LogInfo("Saving: " + filePath);
				Logger.Instance.LogNewLine();
				_unpacker.Context.DumpedModuleDef.Write(filePath, moduleWriterOptions);
			}
		}

		private static void PrepareAllMethods() {
			const BindingFlags BindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

			foreach (MethodBase methodBase in Assembly.GetExecutingAssembly().ManifestModule.GetTypes().SelectMany(t => Enumerable.Concat<MethodBase>(t.GetMethods(BindingFlags), t.GetConstructors(BindingFlags))).Where(m => !m.IsAbstract && !m.ContainsGenericParameters))
				try {
					RuntimeHelpers.PrepareMethod(methodBase.MethodHandle);
				}
				catch {
				}
		}
	}
}
