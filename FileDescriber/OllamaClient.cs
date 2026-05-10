// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.FileDescriber;

using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using ktsu.Semantics.Paths;

internal static class OllamaClient
{
	private static readonly HttpClient HttpClient = new()
	{
		Timeout = TimeSpan.FromMinutes(10),
	};

	internal static async Task<bool> IsAvailableAsync(AiEndpoint endpoint)
	{
		try
		{
			using HttpResponseMessage response = await HttpClient.GetAsync(new Uri(endpoint.WeakString)).ConfigureAwait(false);
			return response.IsSuccessStatusCode;
		}
		catch (HttpRequestException)
		{
			return false;
		}
		catch (TaskCanceledException)
		{
			return false;
		}
	}

	internal static async Task<string> DescribeImageAsync(AiEndpoint endpoint, AiModelName model, string prompt, AbsoluteFilePath imagePath)
	{
		byte[] imageBytes = await File.ReadAllBytesAsync(imagePath.WeakString).ConfigureAwait(false);
		string base64Image = Convert.ToBase64String(imageBytes);

		OllamaRequest request = new()
		{
			Model = model,
			Prompt = prompt,
			Images = [base64Image],
			Stream = false,
		};

		return await SendRequestAsync(endpoint, request).ConfigureAwait(false);
	}

	internal static async Task<string> DescribeTextAsync(AiEndpoint endpoint, AiModelName model, string prompt, AbsoluteFilePath textPath)
	{
		string fileContent = await File.ReadAllTextAsync(textPath.WeakString).ConfigureAwait(false);

		OllamaRequest request = new()
		{
			Model = model,
			Prompt = $"{prompt}\n\n---\n{fileContent}",
			Stream = false,
		};

		return await SendRequestAsync(endpoint, request).ConfigureAwait(false);
	}

	internal static async Task<string> GenerateAsync(AiEndpoint endpoint, AiModelName model, string prompt)
	{
		OllamaRequest request = new()
		{
			Model = model,
			Prompt = prompt,
			Stream = false,
		};

		return await SendRequestAsync(endpoint, request).ConfigureAwait(false);
	}

	private static async Task<string> SendRequestAsync(AiEndpoint endpoint, OllamaRequest request)
	{
		string jsonContent = JsonSerializer.Serialize(request, OllamaJsonContext.Default.OllamaRequest);
		using StringContent content = new(jsonContent, Encoding.UTF8, "application/json");

		Uri requestUri = new($"{endpoint}/api/generate");
		using HttpResponseMessage response = await HttpClient.PostAsync(requestUri, content).ConfigureAwait(false);
		response.EnsureSuccessStatusCode();

		string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
		OllamaResponse? ollamaResponse = JsonSerializer.Deserialize(responseBody, OllamaJsonContext.Default.OllamaResponse);

		return ollamaResponse?.Response ?? string.Empty;
	}
}

[JsonSerializable(typeof(OllamaRequest))]
[JsonSerializable(typeof(OllamaResponse))]
internal sealed partial class OllamaJsonContext : JsonSerializerContext
{
}

internal sealed class OllamaRequest
{
	[JsonPropertyName("model")]
	public string Model { get; set; } = string.Empty;

	[JsonPropertyName("prompt")]
	public string Prompt { get; set; } = string.Empty;

	[JsonPropertyName("images")]
	public string[] Images { get; set; } = [];

	[JsonPropertyName("stream")]
	public bool Stream { get; set; }
}

internal sealed class OllamaResponse
{
	[JsonPropertyName("response")]
	public string Response { get; set; } = string.Empty;
}
