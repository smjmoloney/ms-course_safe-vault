using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SafeVaultClient.Services;

public class AuthService(HttpClient http, AppAuthStateProvider authStateProvider)
{
	public string? Role { get; private set; }

	public async Task InitializeAsync()
	{
		var state = await authStateProvider.GetAuthenticationStateAsync();
		var token = authStateProvider.Token;
		if (!string.IsNullOrWhiteSpace(token))
		{
			http.DefaultRequestHeaders.Authorization =
				new AuthenticationHeaderValue("Bearer", token);
			Role = state.User.IsInRole("Admin") ? "Admin" : "Employee";
		}
	}

	public async Task<bool> Login(string username, string password)
	{
		var response = await http.PostAsJsonAsync("api/auth/login", new { username, password });
		if (!response.IsSuccessStatusCode)
			return false;

		var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
		if (result?.Token == null)
			return false;

		authStateProvider.SetToken(result.Token);
		await authStateProvider.PersistToken(result.Token);
		Role = result.Role;
		http.DefaultRequestHeaders.Authorization =
			new AuthenticationHeaderValue("Bearer", result.Token);
		return true;
	}

	public async Task Logout()
	{
		authStateProvider.SetToken(null);
		await authStateProvider.PersistToken(null);
		Role = null;
		http.DefaultRequestHeaders.Authorization = null;
	}

	private record LoginResponse(string Token, string Role);
}
