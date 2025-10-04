using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nodsoft.Mercury.Data.Models;

namespace Nodsoft.Mercury.Data.Extensions;

/// <summary>
/// Represents extensions for models.
/// </summary>
public static class ModelExtensions
{
	extension<TEntity, TKey>(EntityTypeBuilder<TEntity> builder) where TEntity : CosmosModelBase<TKey>
	{
		/// <summary>
		/// Configures the entity's common properties for CosmosDB.
		/// </summary>
		public EntityTypeBuilder<TEntity> IsCosmosEntity(string containerName)
		{
			builder.ToContainer(containerName);
			builder.HasPartitionKey(e => e.PartitionKey);
			builder.Property(e => e.ETag).IsETagConcurrency();

			return builder;
		}
	}
}