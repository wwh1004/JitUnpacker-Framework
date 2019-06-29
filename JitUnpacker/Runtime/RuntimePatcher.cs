using System;
using System.Collections.Generic;
using static JitTools.Runtime.RuntimeConstants;

namespace JitTools.Runtime {
	internal static unsafe class RuntimePatcher {
		private static readonly List<PatchInfo> _patchInfos = new List<PatchInfo>();

		static RuntimePatcher() {
			AddCanInlinePatchInfo();
			AddStaticConstructorCheckingPatchInfo();
			AddGenericVariablesCheckingPatchInfo();
		}

		public static void PatchAll() {
			if (!RuntimeEnvironment.Is32Bit)
				throw new NotSupportedException();

			foreach (PatchInfo patchInfo in _patchInfos)
				MarshalEx.Write(patchInfo.Address, patchInfo.Patch);
		}

		public static void RestoreAll() {
			if (!RuntimeEnvironment.Is32Bit)
				throw new NotSupportedException();

			foreach (PatchInfo patchInfo in _patchInfos)
				MarshalEx.Write(patchInfo.Address, patchInfo.Original);
		}

		private static void AddCanInlinePatchInfo() {
			byte[] noInlineCode;

			if (RuntimeEnvironment.IsClr45x)
				// CLR45的CEEInfo::canInline开始使用thiscall调用约定
				noInlineCode = new byte[] {
					0x8B, 0x44, 0x24, 0x0C, // mov     eax, [esp+0xC]
					0x85, 0xC0,             // test    eax, eax
					0x74, 0x03,             // je      label1
					0x83, 0x20, 0x00,       // and     [eax], 0x0
					0x6A, 0xFE,             // label1: push    0xFFFFFFFE
					0x58,		            // pop     eax
					0xC2, 0x0C, 0x00        // ret     0xC
				};
			else
				// CLR20~CLR40的CEEInfo::canInline是stdcall调用约定
				noInlineCode = new byte[] {
					0x8B, 0x44, 0x24, 0x10, // mov     eax, [esp+0x10]
					0x85, 0xC0,             // test    eax, eax
					0x74, 0x03,             // je      label1
					0x83, 0x20, 0x00,       // and     [eax], 0x0
					0x6A, 0xFE,             // label1: push    0xFFFFFFFE
					0x58,		            // pop     eax
					0xC2, 0x10, 0x00  	    // ret     0x10
				};
			_patchInfos.Add(CreatePatchInfo((void*)((byte*)RuntimeEnvironment.ClrModuleHandle + CEEINFO_CANINLINE_RVA), noInlineCode));
		}

		private static void AddStaticConstructorCheckingPatchInfo() {
			byte* address;
			byte[] patch;

			if (RuntimeEnvironment.IsClr4x) {
				address = (byte*)RuntimeEnvironment.ClrModuleHandle + METHODDESC_DOPRESTUB_CALL_ISCLASSCONSTRUCTORTRIGGEREDVIAPRESTUB_RVA;
				patch = new byte[] { 0xB8, 0x00, 0x00, 0x00, 0x00, 0x90, 0x90, 0x90 };
				// 85C0          | test    eax, eax            |
				// 0F85 90500300 | jne     clr_4.0_32.78236DAB |
				// 改成：
				// mov eax, 0
				// nop
				// nop
				// nop
			}
			else {
				address = (byte*)RuntimeEnvironment.ClrModuleHandle + METHODDESC_DOPRESTUB_CALL_CHECKRUNCLASSINITTHROWING_RVA;
				patch = new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 };
				//MarshalEx.Write((byte*)RuntimeEnvironment.ClrModuleHandle + 0x1030E, new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 });
				// 8B4D E8     | mov     ecx, dword ptr [ebp-0x18]                                             |
				// E8 D89DFFFF | call    <mscorwks.public: void __thiscall MethodTable::CheckRunClassInitThrow |
				// 改成：
				// nop * 8
			}
			_patchInfos.Add(CreatePatchInfo(address, patch));
		}

		private static void AddGenericVariablesCheckingPatchInfo() {
			if (RuntimeEnvironment.IsClr45x) {
				_patchInfos.Add(CreatePatchInfo((byte*)RuntimeEnvironment.ClrModuleHandle + METHODDESC_DOPRESTUB_CALL_CONTAINSGENERICVARIABLES_RVA, new byte[] { 0xB8, 0x00, 0x00, 0x00, 0x00, 0x90, 0x90, 0x90 }));
				// 85C0          | test    eax, eax     |
				// 0F85 6A313100 | jne     clr.70ABE175 |
				// 改成：
				// mov eax, 0
				// nop
				// nop
				// nop
			}
		}

		private static PatchInfo CreatePatchInfo(void* address, byte[] patch) {
			byte[] original;

			original = new byte[patch.Length];
			MarshalEx.Read(address, original);
			return new PatchInfo {
				Address = address,
				Original = original,
				Patch = patch
			};
		}

		private sealed class PatchInfo {
			public void* Address;
			public byte[] Original;
			public byte[] Patch;
		}
	}
}
