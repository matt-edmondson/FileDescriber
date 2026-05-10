// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.FileDescriber;

using ktsu.Semantics.Strings;

internal sealed record OllamaEndpoint : SemanticString<OllamaEndpoint>;

internal sealed record OllamaModelName : SemanticString<OllamaModelName>;

internal sealed record DescriptionPrompt : SemanticString<DescriptionPrompt>;

internal sealed record FileNamePrompt : SemanticString<FileNamePrompt>;

/// <summary>
/// The backend AI provider to use for inference.
/// </summary>
internal enum BackendType
{
	/// <summary>
	/// Ollama (default) — uses the /api/generate endpoint.
	/// </summary>
	Ollama,

	/// <summary>
	/// OpenAI-compatible API — covers standard OpenAI and LocalAI (localai.io),
	/// both of which expose /v1/chat/completions.
	/// </summary>
	OpenAi,
}
