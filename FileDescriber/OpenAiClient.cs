// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.FileDescriber;

using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using ktsu.Semantics.Paths;

/// <summary>
/// Thin HTTP client for the OpenAI chat-completions API.
/// Compatible with standard OpenAI (api.openai.com) and any
/// OpenAI-compatible server such as LocalAI (localai.io).
/// </summary>
internal static class OpenAiClient
{
	private static readonly HttpClient HttpClient = new()
	{
		Timeout = TimeSpan.FromMinutes(10),
	};

	/// <summary>
	/// Checks availability by calling <c>GET /v1/models</c>.
	/// </summary>
	internal static async Task<bool> IsAvailableAsync(OllamaEndpoint endpoint, string apiKey)
	{
		try
		{
			using HttpRequestMessage req = BuildRequest(HttpMethod.Get, endpoint, "/v1/models", apiKey, content: null);
			using HttpResponseMessage response = await HttpClient.SendAsync(req).ConfigureAwait(false);
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

	/// <summary>
	/// Describes an image file using <c>POST /v1/chat/completions</c> with a
	/// multipart user message containing the prompt and the image as a data URL.
	/// </summary>
	internal static async Task<string> DescribeImageAsync(
		OllamaEndpoint endpoint,
		string apiKey,
		OllamaModelName model,
		string prompt,
		AbsoluteFilePath imagePath)
	{
		byte[] imageBytes = await File.ReadAllBytesAsync(imagePath.WeakString).ConfigureAwait(false);
		string base64Image = Convert.ToBase64String(imageBytes);
		string mimeType = GetImageMimeType(imagePath.FileExtension.WeakString);
		string dataUrl = $"data:{mimeType};base64,{base64Image}";

		OpenAiImageRequest request = new()
		{
			Model = model,
			Stream = false,
			Messages =
			[
				new()
				{
					Role = "user",
					Content =
					[
						new() { Type = "text", Text = prompt },
						new() { Type = "image_url", ImageUrl = new() { Url = dataUrl } },
					],
				},
			],
		};

		string json = JsonSerializer.Serialize(request, OpenAiJsonContext.Default.OpenAiImageRequest);
		return await SendAsync(endpoint, apiKey, json).ConfigureAwait(false);
	}

	/// <summary>
	/// Summarises a text file using <c>POST /v1/chat/completions</c>.
	/// The file content is appended to the prompt inside the user message.
	/// </summary>
	internal static async Task<string> DescribeTextAsync(
		OllamaEndpoint endpoint,
		string apiKey,
		OllamaModelName model,
		string prompt,
		AbsoluteFilePath textPath)
	{
		string fileContent = await File.ReadAllTextAsync(textPath.WeakString).ConfigureAwait(false);

		OpenAiTextRequest request = new()
		{
			Model = model,
			Stream = false,
			Messages =
			[
				new() { Role = "user", Content = $"{prompt}\n\n---\n{fileContent}" },
			],
		};

		string json = JsonSerializer.Serialize(request, OpenAiJsonContext.Default.OpenAiTextRequest);
		return await SendAsync(endpoint, apiKey, json).ConfigureAwait(false);
	}

	/// <summary>
	/// Sends an arbitrary text prompt and returns the model's reply.
	/// </summary>
	internal static async Task<string> GenerateAsync(
		OllamaEndpoint endpoint,
		string apiKey,
		OllamaModelName model,
		string prompt)
	{
		OpenAiTextRequest request = new()
		{
			Model = model,
			Stream = false,
			Messages =
			[
				new() { Role = "user", Content = prompt },
			],
		};

		string json = JsonSerializer.Serialize(request, OpenAiJsonContext.Default.OpenAiTextRequest);
		return await SendAsync(endpoint, apiKey, json).ConfigureAwait(false);
	}

	// -------------------------------------------------------------------------

	private static async Task<string> SendAsync(OllamaEndpoint endpoint, string apiKey, string jsonBody)
	{
		using StringContent body = new(jsonBody, Encoding.UTF8, "application/json");
		using HttpRequestMessage req = BuildRequest(HttpMethod.Post, endpoint, "/v1/chat/completions", apiKey, body);
		using HttpResponseMessage response = await HttpClient.SendAsync(req).ConfigureAwait(false);
		response.EnsureSuccessStatusCode();

		string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
		OpenAiResponse? parsed = JsonSerializer.Deserialize(responseBody, OpenAiJsonContext.Default.OpenAiResponse);

		return parsed?.Choices is { Length: > 0 }
			? parsed.Choices[0].Message.Content
			: string.Empty;
	}

	private static HttpRequestMessage BuildRequest(
		HttpMethod method,
		OllamaEndpoint endpoint,
		string path,
		string apiKey,
		HttpContent? content)
	{
		string baseUrl = endpoint.WeakString.TrimEnd('/');
		HttpRequestMessage req = new(method, new Uri($"{baseUrl}{path}"));

		if (!string.IsNullOrWhiteSpace(apiKey))
		{
			req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
		}

		if (content is not null)
		{
			req.Content = content;
		}

		return req;
	}

	private static string GetImageMimeType(string extension) =>
		extension.ToLowerInvariant() switch
		{
			".jpg" or ".jpeg" => "image/jpeg",
			".png" => "image/png",
			".gif" => "image/gif",
			".bmp" => "image/bmp",
			".webp" => "image/webp",
			".tiff" or ".tif" => "image/tiff",
			_ => "image/jpeg",
		};
}

// ---------------------------------------------------------------------------
// Request / response POCOs
// ---------------------------------------------------------------------------

[JsonSerializable(typeof(OpenAiTextRequest))]
[JsonSerializable(typeof(OpenAiImageRequest))]
[JsonSerializable(typeof(OpenAiResponse))]
internal sealed partial class OpenAiJsonContext : JsonSerializerContext
{
}

internal sealed class OpenAiTextRequest
{
	[JsonPropertyName("model")]
	public string Model { get; set; } = string.Empty;

	[JsonPropertyName("messages")]
	public OpenAiTextMessage[] Messages { get; set; } = [];

	[JsonPropertyName("stream")]
	public bool Stream { get; set; }
}

internal sealed class OpenAiTextMessage
{
	[JsonPropertyName("role")]
	public string Role { get; set; } = string.Empty;

	[JsonPropertyName("content")]
	public string Content { get; set; } = string.Empty;
}

internal sealed class OpenAiImageRequest
{
	[JsonPropertyName("model")]
	public string Model { get; set; } = string.Empty;

	[JsonPropertyName("messages")]
	public OpenAiImageMessage[] Messages { get; set; } = [];

	[JsonPropertyName("stream")]
	public bool Stream { get; set; }
}

internal sealed class OpenAiImageMessage
{
	[JsonPropertyName("role")]
	public string Role { get; set; } = string.Empty;

	[JsonPropertyName("content")]
	public OpenAiContentPart[] Content { get; set; } = [];
}

internal sealed class OpenAiContentPart
{
	[JsonPropertyName("type")]
	public string Type { get; set; } = string.Empty;

	[JsonPropertyName("text")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Text { get; set; }

	[JsonPropertyName("image_url")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public OpenAiImageUrl? ImageUrl { get; set; }
}

internal sealed class OpenAiImageUrl
{
	[JsonPropertyName("url")]
	public string Url { get; set; } = string.Empty;
}

internal sealed class OpenAiResponse
{
	[JsonPropertyName("choices")]
	public OpenAiChoice[] Choices { get; set; } = [];
}

internal sealed class OpenAiChoice
{
	[JsonPropertyName("message")]
	public OpenAiChoiceMessage Message { get; set; } = new();
}

internal sealed class OpenAiChoiceMessage
{
	[JsonPropertyName("content")]
	public string Content { get; set; } = string.Empty;
}
