using System.Runtime.InteropServices;
using System.Text;

namespace JitTools {
	internal static unsafe class NativeMethods {
		public const uint SYMOPT_DEFERRED_LOADS = 4;
		public const uint MAX_PATH = 260;

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern void* GetCurrentProcess();

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern void* GetModuleHandle(string lpModuleName);

		[DllImport("dbghelp.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern uint SymSetOptions(uint SymOptions);

		[DllImport("dbghelp.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SymInitialize(void* hProcess, string UserSearchPath, bool fInvadeProcess);

		[DllImport("dbghelp.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern ulong SymLoadModuleEx(void* hProcess, void* hFile, string ImageName, string ModuleName, ulong BaseOfDll, uint DllSize, void* Data, uint Flags);

		[DllImport("dbghelp.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SymFromName(void* hProcess, string Name, SYMBOL_INFO* Symbol);

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetModuleFileName(void* hModule, StringBuilder lpFilename, uint nSize);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct SYMBOL_INFO {
			public uint SizeOfStruct;
			public uint TypeIndex;
			public fixed ulong Reserved[2];
			public uint Index;
			public uint Size;
			public ulong ModBase;
			public uint Flags;
			public ulong Value;
			public ulong Address;
			public uint Register;
			public uint Scope;
			public uint Tag;
			public uint NameLen;
			public uint MaxNameLen;
			public fixed char Name[1000];
		}
	}
}
