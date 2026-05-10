// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.FileDescriber;

using System.Collections.Generic;
using System.Threading;

using ktsu.AppDataStorage;
using ktsu.Semantics.Strings;

internal sealed class PersistentState : AppData<PersistentState>
{
	public Dictionary<string, FileDescription> Descriptions { get; set; } = [];
	public List<OllamaEndpoint> OllamaEndpoints { get; set; } = ["https://ollama.local.ktsu.dev".As<OllamaEndpoint>()];
	public OllamaModelName OllamaModel { get; set; } = "gemma3:27b".As<OllamaModelName>();
	public int MaxConcurrentRequests { get; set; } = 1;

	private int _endpointRoundRobinIndex = -1;

	internal OllamaEndpoint GetNextEndpoint()
	{
		int count = OllamaEndpoints.Count;
		if (count == 0)
		{
			throw new InvalidOperationException("No Ollama endpoints are configured.");
		}

		int next = Interlocked.Increment(ref _endpointRoundRobinIndex);
		return OllamaEndpoints[(int)((uint)next % (uint)count)];
	}

	internal static OllamaEndpoint GetNextEndpoint(IReadOnlyList<OllamaEndpoint> endpoints)
	{
		ArgumentNullException.ThrowIfNull(endpoints);
		if (endpoints.Count == 0)
		{
			throw new InvalidOperationException("No Ollama endpoints are configured.");
		}

		// Thread-local incrementing counter for caller-supplied lists.
		long next = Interlocked.Increment(ref s_sharedEndpointIndex);
		return endpoints[(int)((ulong)next % (ulong)endpoints.Count)];
	}

	private static long s_sharedEndpointIndex = -1;

	// Per-model, per-type description prompts.
	// Outer key: model name (semantic type). Inner key: FileType enum. Value: typed prompt.
	// When a model+type entry is absent the built-in defaults below are used.
	public Dictionary<OllamaModelName, Dictionary<FileType, DescriptionPrompt>> DescriptionPrompts { get; set; } = new()
	{
		["gemma3:27b".As<OllamaModelName>()] = new()
		{
			[FileType.Image] =
				(
					"Look at this image carefully and write a single paragraph describing what you see. " +
					"Include the main subject, setting, colours, mood, and any notable details. " +
					"Write in plain prose only — no bullet points, headers, or labels. " +
					"Do not begin with phrases like 'This image shows' or 'I can see'."
				).As<DescriptionPrompt>(),

			[FileType.Text] =
				(
					"Read the following text carefully and write a single paragraph summarising its content, " +
					"purpose, and key points. Write in plain prose only — no bullet points, headers, or labels. " +
					"Do not begin with phrases like 'This document' or 'This file'."
				).As<DescriptionPrompt>(),
		},
	};

	// Per-model filename suggestion prompts.
	// Key: model name (semantic type). Value: typed prompt.
	// When a model entry is absent DefaultFileNamePrompt is used.
	public Dictionary<OllamaModelName, FileNamePrompt> FileNamePrompts { get; set; } = new()
	{
		["gemma3:27b".As<OllamaModelName>()] =
			(
				"Based on the description above, output a single filename stem. " +
				"Use only lowercase letters, digits, and hyphens. " +
				"Output nothing else — no extension, no explanation, no punctuation."
			).As<FileNamePrompt>(),
	};

	// -------------------------------------------------------------------------
	// Built-in defaults — generally applicable across common models.
	// Used when no model-specific prompt has been configured.
	// -------------------------------------------------------------------------

	internal static DescriptionPrompt GetDefaultDescriptionPrompt(FileType fileType) => fileType switch
	{
		FileType.Image =>
			(
				"Describe this image in plain prose. Write only the description itself with no labels, " +
				"field names, headings, bullet points, or commentary. " +
				"Do not start with phrases like 'This image shows' or 'The image depicts'."
			).As<DescriptionPrompt>(),

		FileType.Text =>
			(
				"Summarize the content of this text file in plain prose. Write only the summary itself " +
				"with no labels, field names, headings, bullet points, or commentary. " +
				"Do not start with phrases like 'This file contains' or 'The document describes'."
			).As<DescriptionPrompt>(),

		_ =>
			(
				"Describe this file in plain prose. Write only the description itself with no labels, " +
				"field names, headings, bullet points, or commentary."
			).As<DescriptionPrompt>(),
	};

	internal static readonly FileNamePrompt DefaultFileNamePrompt =
		(
			"Based on the description above, suggest a short descriptive filename. " +
			"Respond with only the filename without extension, path, or explanation. " +
			"Use lowercase words separated by hyphens."
		).As<FileNamePrompt>();

	// -------------------------------------------------------------------------
	// Lookup helpers — prefer model-specific, fall back to built-in defaults.
	// -------------------------------------------------------------------------

	internal DescriptionPrompt GetDescriptionPrompt(OllamaModelName model, FileType fileType)
	{
		if (DescriptionPrompts.TryGetValue(model, out Dictionary<FileType, DescriptionPrompt>? typeDict) &&
			typeDict.TryGetValue(fileType, out DescriptionPrompt? prompt) &&
			!string.IsNullOrWhiteSpace(prompt.WeakString))
		{
			return prompt;
		}

		return GetDefaultDescriptionPrompt(fileType);
	}

	internal FileNamePrompt GetFileNamePrompt(OllamaModelName model)
	{
		if (FileNamePrompts.TryGetValue(model, out FileNamePrompt? prompt) &&
			!string.IsNullOrWhiteSpace(prompt.WeakString))
		{
			return prompt;
		}

		return DefaultFileNamePrompt;
	}

	internal void SetDescriptionPrompt(OllamaModelName model, FileType fileType, DescriptionPrompt prompt)
	{
		if (!DescriptionPrompts.TryGetValue(model, out Dictionary<FileType, DescriptionPrompt>? typeDict))
		{
			typeDict = [];
			DescriptionPrompts[model] = typeDict;
		}

		typeDict[fileType] = prompt;
	}

	internal void SetFileNamePrompt(OllamaModelName model, FileNamePrompt prompt) =>
		FileNamePrompts[model] = prompt;
}
