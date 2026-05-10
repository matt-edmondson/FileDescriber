// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.FileDescriber;

using System.Collections.Generic;
using System.IO;

using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

internal static class FileScanner
{
	private static readonly HashSet<FileExtension> ImageExtensions =
	[
		".jpg".As<FileExtension>(),
		".jpeg".As<FileExtension>(),
		".png".As<FileExtension>(),
		".gif".As<FileExtension>(),
		".bmp".As<FileExtension>(),
		".webp".As<FileExtension>(),
		".tiff".As<FileExtension>(),
		".tif".As<FileExtension>(),
	];

	private static readonly HashSet<FileExtension> TextExtensions =
	[
		".txt".As<FileExtension>(),
		".md".As<FileExtension>(),
		".json".As<FileExtension>(),
		".ini".As<FileExtension>(),
		".html".As<FileExtension>(),
		".htm".As<FileExtension>(),
		".yaml".As<FileExtension>(),
		".yml".As<FileExtension>(),
		".xml".As<FileExtension>(),
		".csv".As<FileExtension>(),
		".toml".As<FileExtension>(),
		".log".As<FileExtension>(),
	];

	internal static bool IsImageExtension(FileExtension ext) => ImageExtensions.Contains(ext.WeakString.ToLowerInvariant().As<FileExtension>());

	internal static bool IsTextExtension(FileExtension ext) => TextExtensions.Contains(ext.WeakString.ToLowerInvariant().As<FileExtension>());

	internal static FileType? GetFileType(FileExtension ext)
	{
		FileExtension normalized = ext.WeakString.ToLowerInvariant().As<FileExtension>();

		if (ImageExtensions.Contains(normalized))
		{
			return FileType.Image;
		}

		if (TextExtensions.Contains(normalized))
		{
			return FileType.Text;
		}

		return null;
	}

	internal static IReadOnlyList<AbsoluteFilePath> ScanForFiles(AbsoluteDirectoryPath path)
	{
		if (!path.Exists)
		{
			Console.WriteLine($"Directory not found: {path}");
			return [];
		}

		List<AbsoluteFilePath> files = [];
		foreach (string file in Directory.EnumerateFiles(path.WeakString, "*", SearchOption.AllDirectories))
		{
			string ext = Path.GetExtension(file);
			if (string.IsNullOrEmpty(ext))
			{
				continue;
			}

			FileExtension fileExtension = ext.ToLowerInvariant().As<FileExtension>();
			if (ImageExtensions.Contains(fileExtension) || TextExtensions.Contains(fileExtension))
			{
				files.Add(file.As<AbsoluteFilePath>());
			}
		}

		return files;
	}

	internal static IReadOnlyList<AbsoluteFilePath> ScanForImages(AbsoluteDirectoryPath path)
	{
		if (!path.Exists)
		{
			Console.WriteLine($"Directory not found: {path}");
			return [];
		}

		List<AbsoluteFilePath> imageFiles = [];
		foreach (string file in Directory.EnumerateFiles(path.WeakString, "*", SearchOption.AllDirectories))
		{
			string ext = Path.GetExtension(file);
			if (string.IsNullOrEmpty(ext))
			{
				continue;
			}

			if (ImageExtensions.Contains(ext.ToLowerInvariant().As<FileExtension>()))
			{
				imageFiles.Add(file.As<AbsoluteFilePath>());
			}
		}

		return imageFiles;
	}

	internal static IReadOnlyList<AbsoluteFilePath> ScanForTextFiles(AbsoluteDirectoryPath path)
	{
		if (!path.Exists)
		{
			Console.WriteLine($"Directory not found: {path}");
			return [];
		}

		List<AbsoluteFilePath> textFiles = [];
		foreach (string file in Directory.EnumerateFiles(path.WeakString, "*", SearchOption.AllDirectories))
		{
			string ext = Path.GetExtension(file);
			if (string.IsNullOrEmpty(ext))
			{
				continue;
			}

			if (TextExtensions.Contains(ext.ToLowerInvariant().As<FileExtension>()))
			{
				textFiles.Add(file.As<AbsoluteFilePath>());
			}
		}

		return textFiles;
	}
}
