namespace Nodsoft.Mercury.Models;

/// <summary>
/// Represents the DTO for a form source access token.
/// Most users will only make use off the <see cref="Id" /> to authenticate submissions.
/// </summary>
public sealed class FormSourceAccessTokenDto
{
	/// <summary>
	/// The ID of the access token.
	/// </summary>
	public required Guid Id { get; set; } = Guid.CreateVersion7();
	
	/// <summary>
	/// The form source ID.
	/// </summary>
	/// <seealso cref="FormSourceDto" />
	///
	public required Guid FormSourceId { get; set; }
	
	/// <summary>
	/// The timestamp of the token's creation.
	/// </summary>
	public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
	
	/// <summary>
	/// The timestamp of the token's expiration.
	/// </summary>
	///	<remarks>
	/// If null, the token never expires until revoked.
	/// </remarks>
	public DateTimeOffset? Expires { get; set; }
	
	/// <summary>
	/// Whether or not the token has been revoked.
	/// </summary>
	public bool Revoked { get; set; } = false;
}