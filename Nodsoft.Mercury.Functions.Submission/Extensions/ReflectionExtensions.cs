using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Microsoft.Azure.Functions.Worker;
using Throw;

namespace Nodsoft.Mercury.Functions.Submission.Extensions;

/// <summary>
/// Various utilities for working with reflection.
/// </summary>
public static class ReflectionExtensions
{
	private static readonly BindingFlags DefaultBindingFlags =
		BindingFlags.Public |
		BindingFlags.Instance |
		BindingFlags.Static |
		BindingFlags.FlattenHierarchy;

	/// <summary>
	/// Gets the <see cref="System.Reflection.MethodInfo"/> out of a fully qualified method name.
	/// </summary>
	/// <remarks>
	///	This method will only lookup public methods, in this assembly.
	/// </remarks>
	/// <param name="methodPath">The fully qualified method name (w/ namespace).</param>
	/// <returns>The <see cref="System.Reflection.MethodInfo"/> for the method.</returns>
	/// <exception cref="System.ArgumentException">Thrown if <paramref name="methodPath"/> is empty or not a valid fully qualified method name.</exception>
	/// <exception cref="System.Reflection.AmbiguousMatchException">Thrown if multiple methods match the <paramref name="methodPath"/>.</exception>
	[RequiresUnreferencedCode("Uses reflection on types and methods which may be trimmed in AOT scenarios.")]
	public static MethodInfo GetMethodInfo(string methodPath)
	{
		methodPath.Throw().IfEmpty();

		int lastDot = methodPath.LastIndexOf('.');
		if (lastDot <= 0 || lastDot == methodPath.Length - 1)
		{
			throw new ArgumentException("Invalid fully qualified method name.", nameof(methodPath));
		}

		string typeName = methodPath[..lastDot];
		string methodName = methodPath[(lastDot + 1)..];

		// Try to resolve the type safely (important for Azure / Aspire multi-assembly scenarios)
		Type? classType =
			Type.GetType(typeName, throwOnError: false) ??
			AppDomain.CurrentDomain
				.GetAssemblies()
				.Select(a => a.GetType(typeName, false))
				.FirstOrDefault(t => t != null);

		classType.ThrowIfNull($"Unable to resolve type '{typeName}'.");

		MethodInfo[] methods = classType
			.GetMethods(DefaultBindingFlags)
			.Where(m => m.Name == methodName)
			.ToArray();

		if (methods.Length == 0)
			throw new ArgumentException($"Method '{methodName}' not found on type '{typeName}'.");

		if (methods.Length > 1)
			throw new AmbiguousMatchException($"Multiple methods named '{methodName}' found on type '{typeName}'.");

		return methods[0];
	}

	/// <summary>
	/// Retrieves an attribute from a function, falling back to its class if not found.
	/// </summary>
	/// <typeparam name="TAttribute">The type of attribute to retrieve.</typeparam>
	/// <param name="context">The function context.</param>
	/// <param name="fallbackToClass">Whether to fallback to the class if the attribute is not found on the function method itself.</param>
	/// <returns>The attribute, or null if not found.</returns>
	[Pure]
	[RequiresUnreferencedCode("Uses reflection which may be trimmed in AOT scenarios.")]
	public static TAttribute? GetFunctionAttribute<TAttribute>(
		this FunctionContext context,
		bool fallbackToClass = false)
		where TAttribute : Attribute
		=> context.GetFunctionAttribute(typeof(TAttribute), fallbackToClass) as TAttribute;

	/// <summary>
	/// Retrieves an attribute from a function, falling back to its class if not found.
	/// </summary>
	/// <param name="context">The function context.</param>
	/// <param name="type">The type of attribute to retrieve.</param>
	/// <param name="fallbackToClass">Whether to fallback to the class if the attribute is not found on the function method itself.</param>
	/// <returns>The attribute, or null if not found.</returns>
	/// <exception cref="ArgumentException">Thrown if <paramref name="type"/> is not an attribute.</exception>
	[Pure]
	[RequiresUnreferencedCode("Uses reflection which may be trimmed in AOT scenarios.")]
	public static Attribute? GetFunctionAttribute(
		this FunctionContext context,
		Type type,
		bool fallbackToClass = false)
	{
		type.Throw("The type must be an attribute.")
			.IfFalse(static t => typeof(Attribute).IsAssignableFrom(t));

		MethodInfo methodInfo = GetMethodInfo(context.FunctionDefinition.EntryPoint);

		// Method level
		Attribute? attribute = methodInfo.GetCustomAttribute(type, inherit: true);

		// Class fallback
		if (attribute is null && fallbackToClass)
		{
			attribute = methodInfo.DeclaringType?.GetCustomAttribute(type, inherit: true);
		}

		return attribute;
	}

	/// <summary>
	/// Retrieves all matching attributes from a function, falling back to its class if not found, and then its assembly.
	/// </summary>
	/// <typeparam name="TAttribute">The type of attribute to retrieve.</typeparam>
	/// <param name="context">The function context.</param>
	/// <param name="fallbackToClass">Whether to fallback to the class if the attribute is not found on the function method itself.</param>
	/// <param name="fallbackToAssembly">Whether to fallback to the assembly if the attribute is not found on the function method or class.</param>
	/// <returns>The attributes, or an empty array if not found.</returns>
	[Pure]
	[RequiresUnreferencedCode("Uses reflection which may be trimmed in AOT scenarios.")]
	public static TAttribute[] GetFunctionAttributes<TAttribute>(
		this FunctionContext context,
		bool fallbackToClass = false,
		bool fallbackToAssembly = false)
		where TAttribute : Attribute
	{
		MethodInfo methodInfo = GetMethodInfo(context.FunctionDefinition.EntryPoint);

		TAttribute[] methodAttributes =
			methodInfo.GetCustomAttributes<TAttribute>(inherit: true).ToArray();

		if (methodAttributes.Length != 0)
			return methodAttributes;

		if (fallbackToClass && methodInfo.DeclaringType is not null)
		{
			TAttribute[] classAttributes =
				methodInfo.DeclaringType.GetCustomAttributes<TAttribute>(inherit: true).ToArray();

			if (classAttributes.Length != 0)
				return classAttributes;
		}

		if (fallbackToAssembly && methodInfo.DeclaringType?.Assembly is Assembly assembly)
		{
			TAttribute[] assemblyAttributes =
				assembly.GetCustomAttributes<TAttribute>().ToArray();

			if (assemblyAttributes.Length != 0)
				return assemblyAttributes;
		}

		return Array.Empty<TAttribute>();
	}
}
