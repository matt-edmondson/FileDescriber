// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.FileDescriber;

using System.Threading.Tasks;

using ktsu.Semantics.Paths;

/// <summary>
/// Façade that routes AI inference calls to the correct backend
/// (<see cref="BackendType.Ollama"/> or <see cref="BackendType.OpenAi"/>)
/// based on the configured <see cref="PersistentState.BackendType"/>.
/// </summary>
internal static class AiClient
{
	internal static Task<bool> IsAvailableAsync(OllamaEndpoint endpoint) =>
		Program.Settings.BackendType switch
		{
			BackendType.OpenAi => OpenAiClient.IsAvailableAsync(endpoint, Program.Settings.ApiKey),
			_ => OllamaClient.IsAvailableAsync(endpoint),
		};

	internal static Task<string> DescribeImageAsync(
		OllamaEndpoint endpoint,
		OllamaModelName model,
		string prompt,
		AbsoluteFilePath imagePath) =>
		Program.Settings.BackendType switch
		{
			BackendType.OpenAi => OpenAiClient.DescribeImageAsync(endpoint, Program.Settings.ApiKey, model, prompt, imagePath),
			_ => OllamaClient.DescribeImageAsync(endpoint, model, prompt, imagePath),
		};

	internal static Task<string> DescribeTextAsync(
		OllamaEndpoint endpoint,
		OllamaModelName model,
		string prompt,
		AbsoluteFilePath textPath) =>
		Program.Settings.BackendType switch
		{
			BackendType.OpenAi => OpenAiClient.DescribeTextAsync(endpoint, Program.Settings.ApiKey, model, prompt, textPath),
			_ => OllamaClient.DescribeTextAsync(endpoint, model, prompt, textPath),
		};

	internal static Task<string> GenerateAsync(
		OllamaEndpoint endpoint,
		OllamaModelName model,
		string prompt) =>
		Program.Settings.BackendType switch
		{
			BackendType.OpenAi => OpenAiClient.GenerateAsync(endpoint, Program.Settings.ApiKey, model, prompt),
			_ => OllamaClient.GenerateAsync(endpoint, model, prompt),
		};
}
