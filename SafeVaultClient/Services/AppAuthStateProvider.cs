using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace SafeVaultClient.Services;

public class AppAuthStateProvider(IJSRuntime js) : AuthenticationStateProvider
{
	private const string StorageKey = "auth_token";
	private string? _token;
	private static readonly AuthenticationState Anonymous =
		new(new ClaimsPrincipal(new ClaimsIdentity()));

	public string? Token => _token;

	public void SetToken(string? token)
	{
		_token = token;
		NotifyAuthenticationStateChanged(Task.FromResult(BuildState(token)));
	}

	public async Task PersistToken(string? token)
	{
		if (token == null)
			await js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
		else
			await js.InvokeVoidAsync("localStorage.setItem", StorageKey, token);
	}

	public async override Task<AuthenticationState> GetAuthenticationStateAsync()
	{
		if (_token == null)
		{
			try
			{
				var stored = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
				if (!string.IsNullOrWhiteSpace(stored))
					_token = stored;
			}
			catch
			{
				// JS interop not ready yet
			}
		}
		return BuildState(_token);
	}

	private static AuthenticationState BuildState(string? token)
	{
		if (string.IsNullOrWhiteSpace(token))
			return Anonymous;

		var claims = ParseClaimsFromJwt(token);
		var identity = new ClaimsIdentity(claims, "jwt", nameType: "sub", roleType: "role");
		return new AuthenticationState(new ClaimsPrincipal(identity));
	}

	private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
	{
		var payload = jwt.Split('.')[1];
		var padded = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
		var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
			Convert.FromBase64String(padded));

		if (json == null) yield break;

		foreach (var kvp in json)
		{
			if (kvp.Value.ValueKind == JsonValueKind.Array)
			{
				foreach (var item in kvp.Value.EnumerateArray())
					yield return new Claim(kvp.Key, item.GetString() ?? "");
			}
			else
			{
				yield return new Claim(kvp.Key, kvp.Value.ToString());
			}
		}
	}
}
