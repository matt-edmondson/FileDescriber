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
	public string DescriptionPrompt { get; set; } = "Describe this image in plain prose. Write only the description itself with no labels, field names, headings, bullet points, or commentary. Do not start with phrases like 'This image shows' or 'The image depicts'.";
	public string TextDescriptionPrompt { get; set; } = "Summarize the content of this text file in plain prose. Write only the summary itself with no labels, field names, headings, bullet points, or commentary. Do not start with phrases like 'This file contains' or 'The document describes'.";
	public string SuggestedFileNamePrompt { get; set; } = "Based on the description above, suggest a short descriptive filename. Respond with only the filename without extension, path, or explanation. Use lowercase words separated by hyphens.";
	public int MaxConcurrentRequests { get; set; } = 1;
}
