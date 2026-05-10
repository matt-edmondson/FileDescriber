// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.FileDescriber.Tests;

using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

[TestClass]
public class FileScannerTests
{
	[TestMethod]
	public void ScanForFilesFindsImageFiles()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);

		try
		{
			File.WriteAllBytes(Path.Combine(tempDir, "photo.jpg"), [0xFF, 0xD8]);
			File.WriteAllBytes(Path.Combine(tempDir, "image.png"), [0x89, 0x50]);
			File.WriteAllBytes(Path.Combine(tempDir, "picture.gif"), [0x47, 0x49]);
			File.WriteAllBytes(Path.Combine(tempDir, "bitmap.bmp"), [0x42, 0x4D]);

			IReadOnlyList<AbsoluteFilePath> results = FileScanner.ScanForFiles(tempDir.As<AbsoluteDirectoryPath>());

			Assert.AreEqual(4, results.Count);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[TestMethod]
	public void ScanForFilesFindsTextFiles()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);

		try
		{
			File.WriteAllText(Path.Combine(tempDir, "readme.txt"), "hello");
			File.WriteAllText(Path.Combine(tempDir, "notes.md"), "# Notes");
			File.WriteAllText(Path.Combine(tempDir, "config.json"), "{}");
			File.WriteAllText(Path.Combine(tempDir, "settings.ini"), "[section]");
			File.WriteAllText(Path.Combine(tempDir, "page.html"), "<html/>");
			File.WriteAllText(Path.Combine(tempDir, "config.yaml"), "key: value");

			IReadOnlyList<AbsoluteFilePath> results = FileScanner.ScanForFiles(tempDir.As<AbsoluteDirectoryPath>());

			Assert.AreEqual(6, results.Count);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[TestMethod]
	public void ScanForFilesIgnoresUnsupportedFiles()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);

		try
		{
			File.WriteAllText(Path.Combine(tempDir, "script.cs"), "class A{}");
			File.WriteAllBytes(Path.Combine(tempDir, "binary.exe"), [0x4D, 0x5A]);
			File.WriteAllText(Path.Combine(tempDir, "data.bin"), "data");

			IReadOnlyList<AbsoluteFilePath> results = FileScanner.ScanForFiles(tempDir.As<AbsoluteDirectoryPath>());

			Assert.AreEqual(0, results.Count);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[TestMethod]
	public void ScanForFilesFindsFilesInSubdirectories()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		string subDir = Path.Combine(tempDir, "sub");
		Directory.CreateDirectory(subDir);

		try
		{
			File.WriteAllBytes(Path.Combine(tempDir, "top.jpg"), [0xFF, 0xD8]);
			File.WriteAllText(Path.Combine(subDir, "nested.md"), "# Hello");

			IReadOnlyList<AbsoluteFilePath> results = FileScanner.ScanForFiles(tempDir.As<AbsoluteDirectoryPath>());

			Assert.AreEqual(2, results.Count);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[TestMethod]
	public void ScanForFilesReturnsEmptyForNonExistentDirectory()
	{
		string fakePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent");

		IReadOnlyList<AbsoluteFilePath> results = FileScanner.ScanForFiles(fakePath.As<AbsoluteDirectoryPath>());

		Assert.AreEqual(0, results.Count);
	}

	[TestMethod]
	public void ScanForFilesReturnsEmptyForEmptyDirectory()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);

		try
		{
			IReadOnlyList<AbsoluteFilePath> results = FileScanner.ScanForFiles(tempDir.As<AbsoluteDirectoryPath>());

			Assert.AreEqual(0, results.Count);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[TestMethod]
	public void ScanForImagesRecognizesAllSupportedImageExtensions()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);

		try
		{
			string[] extensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif"];
			foreach (string ext in extensions)
			{
				File.WriteAllBytes(Path.Combine(tempDir, $"file{ext}"), [0x00]);
			}

			IReadOnlyList<AbsoluteFilePath> results = FileScanner.ScanForImages(tempDir.As<AbsoluteDirectoryPath>());

			Assert.AreEqual(extensions.Length, results.Count);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[TestMethod]
	public void ScanForTextFilesRecognizesAllSupportedTextExtensions()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);

		try
		{
			string[] extensions = [".txt", ".md", ".json", ".ini", ".html", ".htm", ".yaml", ".yml", ".xml", ".csv", ".toml", ".log"];
			foreach (string ext in extensions)
			{
				File.WriteAllText(Path.Combine(tempDir, $"file{ext}"), "content");
			}

			IReadOnlyList<AbsoluteFilePath> results = FileScanner.ScanForTextFiles(tempDir.As<AbsoluteDirectoryPath>());

			Assert.AreEqual(extensions.Length, results.Count);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[TestMethod]
	public void GetFileTypeReturnsImageForImageExtensions()
	{
		Assert.AreEqual(FileType.Image, FileScanner.GetFileType(".jpg".As<FileExtension>()));
		Assert.AreEqual(FileType.Image, FileScanner.GetFileType(".png".As<FileExtension>()));
		Assert.AreEqual(FileType.Image, FileScanner.GetFileType(".gif".As<FileExtension>()));
	}

	[TestMethod]
	public void GetFileTypeReturnsTextForTextExtensions()
	{
		Assert.AreEqual(FileType.Text, FileScanner.GetFileType(".txt".As<FileExtension>()));
		Assert.AreEqual(FileType.Text, FileScanner.GetFileType(".md".As<FileExtension>()));
		Assert.AreEqual(FileType.Text, FileScanner.GetFileType(".json".As<FileExtension>()));
		Assert.AreEqual(FileType.Text, FileScanner.GetFileType(".yaml".As<FileExtension>()));
	}

	[TestMethod]
	public void GetFileTypeReturnsNullForUnsupportedExtensions()
	{
		Assert.IsNull(FileScanner.GetFileType(".cs".As<FileExtension>()));
		Assert.IsNull(FileScanner.GetFileType(".exe".As<FileExtension>()));
		Assert.IsNull(FileScanner.GetFileType(".mp3".As<FileExtension>()));
	}
}
