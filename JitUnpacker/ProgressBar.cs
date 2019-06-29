using System;

namespace JitTools {
	internal enum ProgressBarType {
		Character,
		Multicolor
	}

	internal sealed class ProgressBar {
		private readonly int _length;
		private readonly int _width;
		private readonly ProgressBarType _type;
		private readonly int _left;
		private readonly int _top;
		private int _current;

		public int Length => _length;

		public int Width => _width;

		public ProgressBarType Type => _type;

		public int Left => _left;

		public int Top => _top;

		public int Current {
			get => _current;
			set {
				if (value < 0 || value > _length)
					throw new ArgumentOutOfRangeException(nameof(value));

				bool oldCursorVisible;
				int oldCursorLeft;
				int oldCursorTop;

				if (_current == value)
					return;
				oldCursorVisible = Console.CursorVisible;
				oldCursorLeft = Console.CursorLeft;
				oldCursorTop = Console.CursorTop;
				Console.CursorVisible = false;
				_current = value;
				if (_type == ProgressBarType.Multicolor) {
					ConsoleColor backgroundColor;
					ConsoleColor foregroundColor;

					backgroundColor = Console.BackgroundColor;
					foregroundColor = Console.ForegroundColor;
					// 保存背景色与前景色
					Console.BackgroundColor = ConsoleColor.Green;
					Console.SetCursorPosition(_left, _top);
					Console.Write(new string(' ', (int)Math.Round((double)_current / _length * _width)));
					Console.BackgroundColor = backgroundColor;
					// 绘制进度条进度
					Console.ForegroundColor = ConsoleColor.White;
					Console.SetCursorPosition(_left + _width + 1, _top);
					// 更新进度百分比
					Console.Write(((int)Math.Round((double)_current / _length * 100)).ToString() + "%");
					Console.ForegroundColor = foregroundColor;
				}
				else {
					Console.SetCursorPosition(_left + 1, _top);
					Console.Write(new string('*', (int)Math.Round((double)_current / _length * (_width - 2))));
					// 绘制进度条进度
					Console.SetCursorPosition(_left + _width + 1, _top);
					// 显示百分比
					Console.Write(((int)Math.Round((double)_current / _length * 100)).ToString() + "%");
				}
				Console.WriteLine();
				Console.SetCursorPosition(oldCursorLeft, oldCursorTop);
				Console.CursorVisible = oldCursorVisible;
			}
		}

		public ProgressBar(int length) : this(length, 50, ProgressBarType.Character) {
		}

		public ProgressBar(int length, int width, ProgressBarType type) : this(length, width, type, Console.CursorLeft, Console.CursorTop) {
		}

		public ProgressBar(int length, int width, ProgressBarType type, int left, int top) {
			if (length <= 0)
				throw new ArgumentOutOfRangeException(nameof(length));
			if (width <= 0)
				throw new ArgumentOutOfRangeException(nameof(width));

			_length = length;
			_width = width;
			_type = type;
			_left = left;
			_top = top;
			Console.SetCursorPosition(left, top);
			for (int i = left; i < Console.WindowWidth; i++)
				Console.Write(" ");
			// 清空显示区域
			if (_type == ProgressBarType.Multicolor) {
				ConsoleColor backgroundColor;

				backgroundColor = Console.BackgroundColor;
				Console.SetCursorPosition(left, top);
				Console.BackgroundColor = ConsoleColor.White;
				for (int i = 0; i < width; i++)
					Console.Write(" ");
				Console.BackgroundColor = backgroundColor;
			}
			else {
				Console.SetCursorPosition(left, top);
				Console.Write("[");
				Console.SetCursorPosition(left + width - 1, top);
				Console.Write("]");
			}
			// 绘制进度条背景
			Console.WriteLine();
		}
	}
}
