// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.FileDescriber.Verbs;

using System;

using CommandLine;

using ktsu.Semantics.Strings;

[Verb("Configure", HelpText = "Configure the AI backend endpoint, model, and other settings.")]
internal sealed class Configure : BaseVerb<Configure>
{
	internal override void Run(Configure options)
	{
		PrintSettings();

		ConfigureBackend();

		Console.WriteLine();

		ConfigureEndpoints();

		Console.WriteLine();
		Console.Write($"Model [{Program.Settings.AiModel}]: ");
		string? modelInput = Console.ReadLine();
		if (!string.IsNullOrWhiteSpace(modelInput))
		{
			Program.Settings.AiModel = modelInput.Trim().As<AiModelName>();
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
		AiModelName currentModel = Program.Settings.AiModel;
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

	private static void ConfigureBackend()
	{
		Console.WriteLine($"Backend type (current: {Program.Settings.BackendType}):");
		Console.WriteLine("  [1] Ollama");
		Console.WriteLine("  [2] OpenAI-compatible (standard OpenAI or LocalAI)");
		Console.Write("Select backend (Enter to keep current): ");
		string? backendInput = Console.ReadLine();
		if (backendInput?.Trim() == "1")
		{
			Program.Settings.BackendType = BackendType.Ollama;
		}
		else if (backendInput?.Trim() == "2")
		{
			Program.Settings.BackendType = BackendType.OpenAi;
		}

		if (Program.Settings.BackendType == BackendType.OpenAi)
		{
			ConfigureApiKey();
		}
	}

	private static void ConfigureApiKey()
	{
		string maskedKey = string.IsNullOrEmpty(Program.Settings.ApiKey)
			? "(not set)"
			: $"{Program.Settings.ApiKey[..Math.Min(4, Program.Settings.ApiKey.Length)]}…";
		Console.WriteLine();
		Console.Write($"API key [{maskedKey}] (Enter to keep, type 'clear' to remove): ");
		string? keyInput = Console.ReadLine();
		if (keyInput?.Trim().Equals("clear", StringComparison.OrdinalIgnoreCase) == true)
		{
			Program.Settings.ApiKey = string.Empty;
		}
		else if (!string.IsNullOrWhiteSpace(keyInput))
		{
			Program.Settings.ApiKey = keyInput.Trim();
		}
	}

	private static void ConfigureEndpoints()
	{
		Console.WriteLine("Endpoints (current):");
		for (int i = 0; i < Program.Settings.AiEndpoints.Count; i++)
		{
			Console.WriteLine($"  [{i + 1}] {Program.Settings.AiEndpoints[i]}");
		}

		Console.WriteLine();
		string endpointHint = Program.Settings.BackendType == BackendType.OpenAi
			? "e.g. http://localhost:8080 for LocalAI, https://api.openai.com for OpenAI"
			: "e.g. http://localhost:11434";
		Console.Write($"Add endpoint ({endpointHint}) (Enter to skip): ");
		string? addEndpoint = Console.ReadLine();
		while (!string.IsNullOrWhiteSpace(addEndpoint))
		{
			Program.Settings.AiEndpoints.Add(addEndpoint.Trim().As<AiEndpoint>());
			Console.Write("Add another endpoint (Enter to skip): ");
			addEndpoint = Console.ReadLine();
		}

		if (Program.Settings.AiEndpoints.Count > 1)
		{
			Console.Write("Remove endpoint by number (Enter to skip): ");
			string? removeInput = Console.ReadLine();
			while (!string.IsNullOrWhiteSpace(removeInput) &&
				int.TryParse(removeInput.Trim(), out int removeIndex) &&
				removeIndex >= 1 && removeIndex <= Program.Settings.AiEndpoints.Count)
			{
				AiEndpoint removed = Program.Settings.AiEndpoints[removeIndex - 1];
				Program.Settings.AiEndpoints.RemoveAt(removeIndex - 1);
				if (Program.Settings.AiEndpoints.Count == 0)
				{
					Console.WriteLine("Warning: at least one endpoint is required. Restored the removed entry.");
					Program.Settings.AiEndpoints.Add(removed);
					break;
				}

				Console.WriteLine("Remaining endpoints:");
				for (int i = 0; i < Program.Settings.AiEndpoints.Count; i++)
				{
					Console.WriteLine($"  [{i + 1}] {Program.Settings.AiEndpoints[i]}");
				}

				Console.Write("Remove another endpoint by number (Enter to stop): ");
				removeInput = Console.ReadLine();
			}
		}
	}

	private static void PrintSettings()
	{
		AiModelName model = Program.Settings.AiModel;

		Console.WriteLine("Current Settings:");
		Console.WriteLine($"  Backend:         {Program.Settings.BackendType}");

		if (Program.Settings.BackendType == BackendType.OpenAi)
		{
			string maskedKey = string.IsNullOrEmpty(Program.Settings.ApiKey)
				? "(not set)"
				: $"{Program.Settings.ApiKey[..Math.Min(4, Program.Settings.ApiKey.Length)]}…";
			Console.WriteLine($"  API key:         {maskedKey}");
		}

		Console.WriteLine($"  Endpoint(s):");
		foreach (AiEndpoint ep in Program.Settings.AiEndpoints)
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
