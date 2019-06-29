using System;
using System.Cli;
using System.IO;

namespace JitTools {
	public sealed class RuntimeFunctionConfigGeneratorSettings {
		private string _symbolsDirectory;

		[Argument("-d", IsRequired = true, Type = "DIR", Description = "The directory of symbols")]
		internal string SymbolsDirectoryCliSetter {
			set => SymbolsDirectory = value;
		}

		public string SymbolsDirectory {
			get => _symbolsDirectory;
			set {
				if (string.IsNullOrEmpty(value))
					throw new ArgumentNullException(nameof(value));
				if (!Directory.Exists(value))
					throw new DirectoryNotFoundException($"{value} does NOT exists");

				_symbolsDirectory = Path.GetFullPath(value);
			}
		}
	}
}
