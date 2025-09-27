namespace Nodsoft.Mercury.Data.Data;

/// <summary>
/// Represents an access token for a form.
/// These are used to authenticate and authorize form submissions.
/// </summary>
public class FormAccessToken
{
	/// <summary>
	/// The ID of the access token.
	/// 
	/// This serves as the public identifier for the access token,
	/// as well as the primary key in the database.
	/// </summary>
	public required Guid Id { get; set; } = Guid.CreateVersion7();
	
	/// <summary>
	/// The form source ID.
	/// </summary>
	/// <seealso cref="FormSource" />
	///
	public required Guid FormSourceId { get; set; }
	
	/// <summary>
	/// The form source.
	/// </summary>
	/// <seealso cref="FormSourceId" />
	public required FormSource FormSource { get; set; }
	
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