// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.FileDescriber.Verbs;

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using CommandLine;

using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

[Verb("Scan", HelpText = "Scan a directory for files, describe them using Ollama, and store results.")]
internal sealed class Scan : BaseVerb<Scan>
{
	internal override bool ValidateArgs()
	{
		if (PathString is "." or "")
		{
			Console.Write("Enter the path to scan: ");
			string? input = Console.ReadLine()?.Trim();
			if (string.IsNullOrEmpty(input))
			{
				Console.WriteLine("No path provided. Aborting.");
				return false;
			}

			PathString = input;
		}

		return base.ValidateArgs();
	}

	internal override void Run(Scan options)
	{
		Console.WriteLine($"Scanning: {options.Path}");
		Console.WriteLine($"Endpoint(s): {string.Join(", ", options.Endpoints)}");
		Console.WriteLine($"Model: {options.Model}");
		Console.WriteLine();

		// Step 1: Check Ollama availability across all endpoints
		IReadOnlyList<AiEndpoint> endpoints = options.Endpoints;
		if (endpoints.Count == 0)
		{
			Console.WriteLine("Error: No endpoints configured. Use Configure or -e to specify at least one endpoint.");
			return;
		}

		string backendLabel = Program.Settings.BackendType == BackendType.OpenAi ? "OpenAI-compatible" : "Ollama";
		Console.WriteLine($"Checking {backendLabel} availability ({endpoints.Count} endpoint(s))...");
		List<AiEndpoint> availableEndpoints = [];
		foreach (AiEndpoint ep in endpoints)
		{
			bool epAvailable = AiClient.IsAvailableAsync(ep).GetAwaiter().GetResult();
			if (epAvailable)
			{
				Console.WriteLine($"  ✓ {ep}");
				availableEndpoints.Add(ep);
			}
			else
			{
				Console.WriteLine($"  ✗ {ep} (unavailable — skipping)");
			}
		}

		if (availableEndpoints.Count == 0)
		{
			Console.WriteLine($"Error: No {backendLabel} endpoints are available. Make sure the server is running and endpoints are correct.");
			return;
		}

		Console.WriteLine($"Using {availableEndpoints.Count} available endpoint(s).");
		Console.WriteLine();

		// Step 2: Discover all supported files
		Console.WriteLine("Discovering files...");
		IReadOnlyList<AbsoluteFilePath> allFiles = FileScanner.ScanForFiles(options.Path);
		Console.WriteLine($"Found {allFiles.Count} supported file(s).");
		Console.WriteLine();

		if (allFiles.Count == 0)
		{
			return;
		}

		// Step 3: Hash all files in parallel
		Console.WriteLine("Hashing files...");
		Dictionary<AbsoluteFilePath, string> fileHashes = FileHasher.HashFiles(allFiles);
		Console.WriteLine();

		// Step 4: Filter out already-described hashes and deduplicate within this scan
		Dictionary<string, FileDescription> descriptions = Program.Settings.Descriptions;
		Dictionary<string, (List<AbsoluteFilePath> Paths, FileType Type)> newHashPaths = [];
		int skippedCount = 0;
		int newPathCount = 0;
		int duplicateCount = 0;

		foreach (KeyValuePair<AbsoluteFilePath, string> kvp in fileHashes)
		{
			FileType fileType = FileScanner.GetFileType(kvp.Key.FileExtension) ?? FileType.Image;

			if (descriptions.TryGetValue(kvp.Value, out FileDescription? existing))
			{
				skippedCount++;
				if (!existing.KnownPaths.Contains(kvp.Key))
				{
					existing.KnownPaths.Add(kvp.Key);
					newPathCount++;
				}
			}
			else if (newHashPaths.TryGetValue(kvp.Value, out (List<AbsoluteFilePath> Paths, FileType Type) entry))
			{
				entry.Paths.Add(kvp.Key);
				duplicateCount++;
			}
			else
			{
				newHashPaths[kvp.Value] = ([kvp.Key], fileType);
			}
		}

		if (newPathCount > 0)
		{
			Program.Settings.Save();
			Console.WriteLine($"Discovered {newPathCount} new path(s) for existing files.");
		}

		Console.WriteLine($"Unique new files to describe: {newHashPaths.Count}");
		if (duplicateCount > 0)
		{
			Console.WriteLine($"Found {duplicateCount} duplicate(s) that will share descriptions.");
		}

		if (skippedCount > 0)
		{
			Console.WriteLine($"Skipping {skippedCount} already-described file(s).");
		}

		Console.WriteLine();

		// Step 5: Describe new files with configurable concurrency
		FileNamePrompt fileNamePrompt = Program.Settings.GetFileNamePrompt(options.Model);
		int maxConcurrency = Math.Max(1, Program.Settings.MaxConcurrentRequests);
		DescribeFiles(newHashPaths, availableEndpoints, options.Model, fileNamePrompt, maxConcurrency);

		Console.WriteLine("Scan complete.");
		Console.WriteLine($"Total descriptions in database: {Program.Settings.Descriptions.Count}");

		PathString = ".";
	}

	private static void DescribeFiles(
		Dictionary<string, (List<AbsoluteFilePath> Paths, FileType Type)> newHashPaths,
		List<AiEndpoint> availableEndpoints,
		AiModelName model,
		FileNamePrompt fileNamePrompt,
		int maxConcurrency)
	{
		int current = 0;
		int total = newHashPaths.Count;
		Lock consoleLock = new();
		Lock saveLock = new();
		EndpointLoadBalancer balancer = new(availableEndpoints);

		Console.WriteLine($"Processing with {maxConcurrency} concurrent request(s) across {availableEndpoints.Count} endpoint(s)...");
		Console.WriteLine();

		ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = maxConcurrency };
		DateTime jobStart = DateTime.UtcNow;
		int completed = 0;

		Parallel.ForEach(newHashPaths, parallelOptions, kvp =>
		{
			(string hash, (List<AbsoluteFilePath> paths, FileType fileType)) = (kvp.Key, kvp.Value);
			AbsoluteFilePath filePath = paths[0];
			int index = Interlocked.Increment(ref current);

			// Pick the least-loaded endpoint for this request.
			(int endpointIndex, AiEndpoint endpoint) = balancer.Acquire();

			lock (consoleLock)
			{
				Console.WriteLine($"[{index}/{total}] Describing {filePath.FileName} ({fileType}, {paths.Count} copy/copies) via {endpoint}...");
			}

			DateTime requestStart = DateTime.UtcNow;
			TimeSpan requestElapsed = TimeSpan.Zero;

			try
			{
				string pathContext = string.Join("\n", paths.Select(p => p.WeakString));
				string description;
				DescriptionPrompt descriptionPrompt = Program.Settings.GetDescriptionPrompt(model, fileType);

				if (fileType == FileType.Image)
				{
					string fullPrompt = $"Known file paths for this image:\n{pathContext}\n\n{descriptionPrompt.WeakString}";
					description = AiClient.DescribeImageAsync(endpoint, model, fullPrompt, filePath).GetAwaiter().GetResult();
				}
				else
				{
					string fullPrompt = $"Known file paths for this file:\n{pathContext}\n\n{descriptionPrompt.WeakString}";
					description = AiClient.DescribeTextAsync(endpoint, model, fullPrompt, filePath).GetAwaiter().GetResult();
				}

				string combinedFileNamePrompt = $"File description: {description}\n\n{fileNamePrompt.WeakString}";
				string rawSuggestion = AiClient.GenerateAsync(endpoint, model, combinedFileNamePrompt).GetAwaiter().GetResult();
				FileName suggestedFileName = SanitizeFileName(rawSuggestion, filePath.FileExtension);

				requestElapsed = DateTime.UtcNow - requestStart;

				FileDescription entry = new()
				{
					Hash = hash,
					KnownPaths = [.. paths],
					Description = description,
					SuggestedFileName = suggestedFileName,
					Model = model,
					DescribedAt = DateTime.UtcNow,
					FileSizeBytes = new FileInfo(filePath.WeakString).Length,
					FileType = fileType,
				};

				lock (saveLock)
				{
					Program.Settings.Descriptions[hash] = entry;
					Program.Settings.Save();
				}

				int doneCount = Interlocked.Increment(ref completed);
				string eta = FormatEta(DateTime.UtcNow - jobStart, doneCount, total);

				lock (consoleLock)
				{
					Console.WriteLine($"  [{index}/{total}] Suggested: {suggestedFileName}");
					Console.WriteLine($"  [{index}/{total}] Done in {FormatDuration(requestElapsed)} | ETA: {eta}");
					Console.WriteLine($"  [{index}/{total}] {description[..Math.Min(80, description.Length)]}...");
				}
			}
			catch (HttpRequestException ex)
			{
				requestElapsed = DateTime.UtcNow - requestStart;
				int doneCount = Interlocked.Increment(ref completed);
				string eta = FormatEta(DateTime.UtcNow - jobStart, doneCount, total);

				lock (consoleLock)
				{
					Console.WriteLine($"  [{index}/{total}] Error describing {filePath.FileName}: {ex.Message} ({FormatDuration(requestElapsed)}) | ETA: {eta}");
				}
			}
			finally
			{
				balancer.Release(endpointIndex);
			}
		});
	}

	internal static FileName SanitizeFileName(string rawSuggestion, FileExtension extension)
	{
		string name = rawSuggestion.Trim().Trim('"', '\'', '`');

		// Take only the first line if the model returned multiple lines
		int newlineIndex = name.IndexOf('\n', StringComparison.Ordinal);
		if (newlineIndex >= 0)
		{
			name = name[..newlineIndex].Trim();
		}

		// Strip any extension the model may have included
		string existingExt = System.IO.Path.GetExtension(name);
		if (!string.IsNullOrEmpty(existingExt))
		{
			name = System.IO.Path.GetFileNameWithoutExtension(name);
		}

		// Remove invalid filename characters
		foreach (char c in System.IO.Path.GetInvalidFileNameChars())
		{
			name = name.Replace(c, '-');
		}

		// Collapse multiple hyphens and trim
		while (name.Contains("--", StringComparison.Ordinal))
		{
			name = name.Replace("--", "-", StringComparison.Ordinal);
		}

		name = name.Trim('-', ' ');

		if (string.IsNullOrEmpty(name))
		{
			name = "unnamed";
		}

		return $"{name}{extension}".As<FileName>();
	}

	private static string FormatDuration(TimeSpan duration)
	{
		if (duration.TotalHours >= 1)
		{
			return $"{(int)duration.TotalHours}h {duration.Minutes}m {duration.Seconds}s";
		}

		if (duration.TotalMinutes >= 1)
		{
			return $"{duration.Minutes}m {duration.Seconds}s";
		}

		return $"{duration.Seconds}.{duration.Milliseconds / 100}s";
	}

	private static string FormatEta(TimeSpan elapsed, int done, int totalCount)
	{
		if (done <= 0 || totalCount <= 0)
		{
			return "calculating...";
		}

		int remaining = totalCount - done;
		if (remaining <= 0)
		{
			return "done";
		}

		TimeSpan avgPerItem = elapsed / done;
		TimeSpan eta = avgPerItem * remaining;
		return FormatDuration(eta);
	}
}
