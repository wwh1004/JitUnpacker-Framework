using System.Cli;
using System.IO;

namespace JitTools {
	public sealed class RuntimeFunctionConfigGeneratorSettings {
		private string _symbolsDirectory;

		[Argument("-d", IsRequired = false, DefaultValue = "", Type = "DIR", Description = "The directory of symbols")]
		internal string SymbolsDirectoryCliSetter {
			set => SymbolsDirectory = value;
		}

		public string SymbolsDirectory {
			get => _symbolsDirectory;
			set => _symbolsDirectory = string.IsNullOrEmpty(value) ? null : Path.GetFullPath(value);
		}
	}
}
