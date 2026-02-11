using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Azure.Functions.Worker;
using Throw;

namespace Nodsoft.Mercury.Functions.Submission.Extensions;

/// <summary>
/// Various utilities for working with reflection.
/// </summary>
public static class ReflectionExtensions
{
	private static readonly Dictionary<string, MethodInfo> _methodCache = new();

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
	public static MethodInfo GetMethodInfo(string methodPath)
	{
		methodPath.Throw().IfEmpty();

		if (_methodCache.TryGetValue(methodPath, out var cached))
			return cached;

		int lastDot = methodPath.LastIndexOf('.');
		if (lastDot <= 0 || lastDot == methodPath.Length - 1)
			throw new ArgumentException("Invalid fully qualified method name.", nameof(methodPath));

		string typeName = methodPath[..lastDot];
		string methodName = methodPath[(lastDot + 1)..];

		Type? classType = AppDomain.CurrentDomain
			.GetAssemblies()
			.Select(a => a.GetType(typeName))
			.FirstOrDefault(t => t is not null);

		classType.ThrowIfNull();

		MethodInfo? methodInfo = classType.GetMethod(
			methodName,
			BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

		methodInfo.ThrowIfNull();

		_methodCache[methodPath] = methodInfo;

		return methodInfo;
	}

	/// <summary>
	/// Retrieves an attribute from a function, falling back to its class if not found.
	/// </summary>
	/// <typeparam name="TAttribute">The type of attribute to retrieve.</typeparam>
	/// <param name="context">The function context.</param>
	/// <param name="fallbackToClass">Whether to fallback to the class if the attribute is not found on the function method itself.</param>
	/// <returns>The attribute, or null if not found.</returns>
	[Pure]
	public static TAttribute? GetFunctionAttribute<TAttribute>(this FunctionContext context, bool fallbackToClass = false)
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
	public static Attribute? GetFunctionAttribute(this FunctionContext context, Type type, bool fallbackToClass = false)
	{
		type.Throw("The type must be an attribute.")
			.IfFalse(t => typeof(Attribute).IsAssignableFrom(t));

		MethodInfo methodInfo = GetMethodInfo(context.FunctionDefinition.EntryPoint);

		Attribute? attribute = methodInfo.GetCustomAttribute(type);
		attribute ??= fallbackToClass
			? methodInfo.DeclaringType?.GetCustomAttribute(type)
			: null;

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
	public static TAttribute[] GetFunctionAttributes<TAttribute>(this FunctionContext context, bool fallbackToClass = false, bool fallbackToAssembly = false)
		where TAttribute : Attribute
	{
		MethodInfo methodInfo = GetMethodInfo(context.FunctionDefinition.EntryPoint);

		var methodAttributes = methodInfo.GetCustomAttributes<TAttribute>().ToArray();
		if (methodAttributes.Length > 0)
			return methodAttributes;

		if (fallbackToClass && methodInfo.DeclaringType is not null)
		{
			var classAttributes = methodInfo.DeclaringType
				.GetCustomAttributes<TAttribute>()
				.ToArray();

			if (classAttributes.Length > 0)
				return classAttributes;
		}

		if (fallbackToAssembly && methodInfo.DeclaringType is not null)
		{
			var assemblyAttributes = methodInfo.DeclaringType.Assembly
				.GetCustomAttributes<TAttribute>()
				.ToArray();

			if (assemblyAttributes.Length > 0)
				return assemblyAttributes;
		}

		return Array.Empty<TAttribute>();
	}
}