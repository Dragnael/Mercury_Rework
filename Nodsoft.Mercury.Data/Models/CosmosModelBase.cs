using System.ComponentModel.DataAnnotations.Schema;

namespace Nodsoft.Mercury.Data.Models;

/// <summary>
/// Represents a model that can be stored in Cosmos DB.
/// </summary>
public abstract record CosmosModelBase<TKey>
{
	/// <summary>
	/// The ID of the model.
	/// </summary>
	public virtual required TKey Id { get; set; }
	
	/// <summary>
	/// The partition key of the model.
	/// </summary>
	[Column(name: "__partitionKey")]
	public required string PartitionKey { get; set; }
	
	/// <summary>
	/// The ETag of the model.
	/// </summary>
	[Column(name: "__etag")]
	public string? ETag { get; set; }
}