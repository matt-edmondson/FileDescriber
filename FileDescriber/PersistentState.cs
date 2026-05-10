// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.FileDescriber;

using System.Collections.Generic;

using ktsu.AppDataStorage;
using ktsu.Semantics.Strings;

internal sealed class PersistentState : AppData<PersistentState>
{
	public Dictionary<string, FileDescription> Descriptions { get; set; } = [];
	public OllamaEndpoint OllamaEndpoint { get; set; } = "https://ollama.local.ktsu.dev".As<OllamaEndpoint>();
	public OllamaModelName OllamaModel { get; set; } = "gemma3:27b".As<OllamaModelName>();
	public int MaxConcurrentRequests { get; set; } = 1;

	// Per-model, per-type description prompts.
	// Outer key: model name. Inner key: FileType.ToString(). Value: prompt text.
	// When a model+type entry is absent the built-in defaults below are used.
	public Dictionary<string, Dictionary<string, string>> DescriptionPrompts { get; set; } = new()
	{
		["gemma3:27b"] = new()
		{
			[nameof(FileType.Image)] =
				"Look at this image carefully and write a single paragraph describing what you see. " +
				"Include the main subject, setting, colours, mood, and any notable details. " +
				"Write in plain prose only — no bullet points, headers, or labels. " +
				"Do not begin with phrases like 'This image shows' or 'I can see'.",

			[nameof(FileType.Text)] =
				"Read the following text carefully and write a single paragraph summarising its content, " +
				"purpose, and key points. Write in plain prose only — no bullet points, headers, or labels. " +
				"Do not begin with phrases like 'This document' or 'This file'.",
		},
	};

	// Per-model filename suggestion prompts.
	// Key: model name. Value: prompt text.
	// When a model entry is absent DefaultFileNamePrompt is used.
	public Dictionary<string, string> FileNamePrompts { get; set; } = new()
	{
		["gemma3:27b"] =
			"Based on the description above, output a single filename stem. " +
			"Use only lowercase letters, digits, and hyphens. " +
			"Output nothing else — no extension, no explanation, no punctuation.",
	};

	// -------------------------------------------------------------------------
	// Built-in defaults — generally applicable across common models.
	// Used when no model-specific prompt has been configured.
	// -------------------------------------------------------------------------

	internal static string GetDefaultDescriptionPrompt(FileType fileType) => fileType switch
	{
		FileType.Image =>
			"Describe this image in plain prose. Write only the description itself with no labels, " +
			"field names, headings, bullet points, or commentary. " +
			"Do not start with phrases like 'This image shows' or 'The image depicts'.",

		FileType.Text =>
			"Summarize the content of this text file in plain prose. Write only the summary itself " +
			"with no labels, field names, headings, bullet points, or commentary. " +
			"Do not start with phrases like 'This file contains' or 'The document describes'.",

		_ =>
			"Describe this file in plain prose. Write only the description itself with no labels, " +
			"field names, headings, bullet points, or commentary.",
	};

	internal const string DefaultFileNamePrompt =
		"Based on the description above, suggest a short descriptive filename. " +
		"Respond with only the filename without extension, path, or explanation. " +
		"Use lowercase words separated by hyphens.";

	// -------------------------------------------------------------------------
	// Lookup helpers — prefer model-specific, fall back to built-in defaults.
	// -------------------------------------------------------------------------

	internal string GetDescriptionPrompt(OllamaModelName model, FileType fileType)
	{
		if (DescriptionPrompts.TryGetValue(model.WeakString, out Dictionary<string, string>? typeDict) &&
			typeDict.TryGetValue(fileType.ToString(), out string? prompt) &&
			!string.IsNullOrWhiteSpace(prompt))
		{
			return prompt;
		}

		return GetDefaultDescriptionPrompt(fileType);
	}

	internal string GetFileNamePrompt(OllamaModelName model)
	{
		if (FileNamePrompts.TryGetValue(model.WeakString, out string? prompt) &&
			!string.IsNullOrWhiteSpace(prompt))
		{
			return prompt;
		}

		return DefaultFileNamePrompt;
	}

	internal void SetDescriptionPrompt(OllamaModelName model, FileType fileType, string prompt)
	{
		if (!DescriptionPrompts.TryGetValue(model.WeakString, out Dictionary<string, string>? typeDict))
		{
			typeDict = [];
			DescriptionPrompts[model.WeakString] = typeDict;
		}

		typeDict[fileType.ToString()] = prompt;
	}

	internal void SetFileNamePrompt(OllamaModelName model, string prompt) =>
		FileNamePrompts[model.WeakString] = prompt;
}
