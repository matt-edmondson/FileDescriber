// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.FileDescriber.Verbs;

using System;

using CommandLine;

using ktsu.Semantics.Strings;

[Verb("Configure", HelpText = "Configure the Ollama endpoint and model settings.")]
internal sealed class Configure : BaseVerb<Configure>
{
	internal override void Run(Configure options)
	{
		PrintSettings();

		// --- Endpoint(s) / model / concurrency ---
		Console.WriteLine("Endpoints (current):");
		for (int i = 0; i < Program.Settings.OllamaEndpoints.Count; i++)
		{
			Console.WriteLine($"  [{i + 1}] {Program.Settings.OllamaEndpoints[i]}");
		}

		Console.WriteLine();
		Console.Write("Add endpoint (Enter to skip): ");
		string? addEndpoint = Console.ReadLine();
		while (!string.IsNullOrWhiteSpace(addEndpoint))
		{
			Program.Settings.OllamaEndpoints.Add(addEndpoint.Trim().As<OllamaEndpoint>());
			Console.Write("Add another endpoint (Enter to skip): ");
			addEndpoint = Console.ReadLine();
		}

		if (Program.Settings.OllamaEndpoints.Count > 1)
		{
			Console.Write("Remove endpoint by number (Enter to skip): ");
			string? removeInput = Console.ReadLine();
			while (!string.IsNullOrWhiteSpace(removeInput) &&
				int.TryParse(removeInput.Trim(), out int removeIndex) &&
				removeIndex >= 1 && removeIndex <= Program.Settings.OllamaEndpoints.Count)
			{
				OllamaEndpoint removed = Program.Settings.OllamaEndpoints[removeIndex - 1];
				Program.Settings.OllamaEndpoints.RemoveAt(removeIndex - 1);
				if (Program.Settings.OllamaEndpoints.Count == 0)
				{
					Console.WriteLine("Warning: at least one endpoint is required. Restored the removed entry.");
					Program.Settings.OllamaEndpoints.Add(removed);
					break;
				}

				Console.WriteLine("Remaining endpoints:");
				for (int i = 0; i < Program.Settings.OllamaEndpoints.Count; i++)
				{
					Console.WriteLine($"  [{i + 1}] {Program.Settings.OllamaEndpoints[i]}");
				}

				Console.Write("Remove another endpoint by number (Enter to stop): ");
				removeInput = Console.ReadLine();
			}
		}

		Console.WriteLine();
		Console.Write($"Ollama Model [{Program.Settings.OllamaModel}]: ");
		string? modelInput = Console.ReadLine();
		if (!string.IsNullOrWhiteSpace(modelInput))
		{
			Program.Settings.OllamaModel = modelInput.Trim().As<OllamaModelName>();
		}

		Console.Write($"Max Concurrent Requests [{Program.Settings.MaxConcurrentRequests}]: ");
		string? concurrencyInput = Console.ReadLine();
		if (!string.IsNullOrWhiteSpace(concurrencyInput) &&
			int.TryParse(concurrencyInput.Trim(), out int concurrency) &&
			concurrency >= 1)
		{
			Program.Settings.MaxConcurrentRequests = concurrency;
		}

		// --- Per-model, per-type prompts for the current model ---
		OllamaModelName currentModel = Program.Settings.OllamaModel;
		Console.WriteLine();
		Console.WriteLine($"Description prompts for model '{currentModel}'");
		Console.WriteLine("(Press Enter to keep the current value. Effective value shown in brackets.)");
		Console.WriteLine();

		foreach (FileType fileType in Enum.GetValues<FileType>())
		{
			DescriptionPrompt effective = Program.Settings.GetDescriptionPrompt(currentModel, fileType);
			string preview = effective.WeakString[..Math.Min(60, effective.WeakString.Length)];
			Console.Write($"  {fileType} [{preview}...]: ");
			string? input = Console.ReadLine();
			if (!string.IsNullOrWhiteSpace(input))
			{
				Program.Settings.SetDescriptionPrompt(currentModel, fileType, input.Trim().As<DescriptionPrompt>());
			}
		}

		Console.WriteLine();
		FileNamePrompt effectiveFileName = Program.Settings.GetFileNamePrompt(currentModel);
		string fileNamePreview = effectiveFileName.WeakString[..Math.Min(60, effectiveFileName.WeakString.Length)];
		Console.Write($"Filename prompt for '{currentModel}' [{fileNamePreview}...]: ");
		string? fileNameInput = Console.ReadLine();
		if (!string.IsNullOrWhiteSpace(fileNameInput))
		{
			Program.Settings.SetFileNamePrompt(currentModel, fileNameInput.Trim().As<FileNamePrompt>());
		}

		Program.Settings.Save();

		Console.WriteLine();
		Console.WriteLine("Settings saved.");
		PrintSettings();
	}

	private static void PrintSettings()
	{
		OllamaModelName model = Program.Settings.OllamaModel;

		Console.WriteLine("Current Settings:");
		Console.WriteLine($"  Endpoint(s):");
		foreach (OllamaEndpoint ep in Program.Settings.OllamaEndpoints)
		{
			Console.WriteLine($"    {ep}");
		}

		Console.WriteLine($"  Model:           {model}");
		Console.WriteLine($"  Concurrency:     {Program.Settings.MaxConcurrentRequests}");
		Console.WriteLine();
		Console.WriteLine($"  Prompts for '{model}':");

		foreach (FileType fileType in Enum.GetValues<FileType>())
		{
			DescriptionPrompt effective = Program.Settings.GetDescriptionPrompt(model, fileType);
			Console.WriteLine($"    {fileType}: {effective.WeakString[..Math.Min(60, effective.WeakString.Length)]}...");
		}

		FileNamePrompt fileNamePrompt = Program.Settings.GetFileNamePrompt(model);
		Console.WriteLine($"    Filename: {fileNamePrompt.WeakString[..Math.Min(60, fileNamePrompt.WeakString.Length)]}...");
		Console.WriteLine();
	}
}
