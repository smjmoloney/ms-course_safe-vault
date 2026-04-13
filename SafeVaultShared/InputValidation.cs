namespace SafeVaultShared;

public static class InputValidation
{
	public static bool IsValidInput(string input, string allowedSpecialChars = "")
	{
		if (string.IsNullOrEmpty(input))
			return false;

		var validChars = allowedSpecialChars.ToHashSet();
		return input.All(c => char.IsLetterOrDigit(c) || validChars.Contains(c));
	}

	public static bool IsValidPassword(string password)
	{
		return IsValidInput(password, "!@#$%^&*?");
	}
}
