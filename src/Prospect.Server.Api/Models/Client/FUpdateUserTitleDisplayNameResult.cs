namespace Prospect.Server.Api.Models.Client;

public class FUpdateUserTitleDisplayNameResult
{
    /// <summary>
    ///     [optional] Current title display name for the user (this will be the original display name if the rename attempt failed).
    /// </summary>
    public string? DisplayName { get; set; }
}