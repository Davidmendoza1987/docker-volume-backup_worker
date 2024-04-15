using Newtonsoft.Json;
using System.ComponentModel;

namespace DockerVolumeBackup.Worker;

/// <summary>
/// Interface for managing and transforming environment variables.
/// </summary>
public interface IEnvironment
{
    /// <summary>
    /// Sets an environment variable to the specified value.
    /// <code>
    /// var wrapper = new EnvironmentWrapper();
    /// wrapper.SetEnvironmentVariable("MY_VAR", "SomeValue");
    /// </code>
    /// </summary>
    /// <param name="name">The name of the environment variable.</param>
    /// <param name="value">The value to set for the environment variable.</param>
    void SetEnvironmentVariable(string name, string value);

    /// <summary>
    /// Gets the value of an environment variable.
    /// <code>
    /// var wrapper = new EnvironmentWrapper();
    /// var value = wrapper.GetEnvironmentVariable("MY_VAR");
    /// </code>
    /// </summary>
    /// <param name="name">The name of the environment variable.</param>
    /// <returns>The value of the environment variable or null if it is not set.</returns>
    string? GetEnvironmentVariable(string name);

    /// <summary>
    /// Converts the value of an environment variable to a specified type.
    /// <code>
    /// var wrapper = new EnvironmentWrapper();
    /// int? port = wrapper.GetEnvironmentVariable&lt;int&gt;("PORT_NUMBER");
    /// </code>
    /// </summary>
    /// <typeparam name="T">The type to convert the value to.</typeparam>
    /// <param name="name">The name of the environment variable.</param>
    /// <returns>The converted value, or null if the conversion is not possible.</returns>
    T? GetEnvironmentVariable<T>(string name) where T : struct;

    /// <summary>
    /// Gets the value of an environment variable, throwing an exception if not set.
    /// <code>
    /// var wrapper = new EnvironmentWrapper();
    /// var value = wrapper.GetRequiredEnvironmentVariable("MY_VAR");
    /// </code>
    /// </summary>
    /// <param name="name">The name of the environment variable.</param>
    /// <returns>The value of the environment variable.</returns>
    string GetRequiredEnvironmentVariable(string name);

    /// <summary>
    /// Converts the value of an environment variable to a specified type, throwing an exception if not set.
    /// <code>
    /// var wrapper = new EnvironmentWrapper();
    /// int port = wrapper.GetRequiredEnvironmentVariable&lt;int&gt;("PORT_NUMBER");
    /// </code>
    /// </summary>
    /// <typeparam name="T">The type to convert the value to.</typeparam>
    /// <param name="name">The name of the environment variable.</param>
    /// <returns>The converted value.</returns>
    T GetRequiredEnvironmentVariable<T>(string name) where T : struct;

    /// <summary>
    /// Parses a comma-delimited environment variable into a collection of a specified type.
    /// <code>
    /// var wrapper = new EnvironmentWrapper();
    /// List&lt;int&gt; values = wrapper.GetEnvironmentVariableCommaDelimitedValuesAsCollection&lt;List&lt;int&gt;, int&gt;("NUMBERS");
    /// </code>
    /// </summary>
    /// <typeparam name="TCollection">The type of collection to return.</typeparam>
    /// <typeparam name="TElement">The type of elements in the collection.</typeparam>
    /// <param name="name">The name of the environment variable.</param>
    /// <returns>A collection of the specified type containing elements parsed from the environment variable.</returns>
    TCollection GetEnvironmentVariableCommaDelimitedValuesAsCollection<TCollection, TElement>(string name)
        where TCollection : ICollection<TElement>, new()
        where TElement : notnull;

    /// <summary>
    /// Deserializes a JSON string from an environment variable into a specified type.
    /// <code>
    /// var wrapper = new EnvironmentWrapper();
    /// var config = wrapper.GetEnvironmentVariableJsonAsType&lt;Config&gt;("CONFIG_JSON");
    /// </code>
    /// </summary>
    /// <typeparam name="T">The type to deserialize the JSON into.</typeparam>
    /// <param name="name">The name of the environment variable containing the JSON string.</param>
    /// <returns>The deserialized object of type T.</returns>
    T GetEnvironmentVariableJsonAsType<T>(string name) where T : class;
}

/// <summary>
/// Represents a utility class for managing and transforming environment variables.
/// This class provides functionality to set and retrieve environment variables,
/// convert environment variable values to different types, and parse environment
/// variable values into collections. It also supports deserializing JSON stored in
/// environment variables into specific types.
/// </summary>
/// <remarks>
/// The <see cref="EnvironmentWrapper"/> class offers a convenient way to interact
/// with the system's environment variables. It abstracts the complexity of type 
/// conversion and parsing operations, facilitating a cleaner and more readable approach
/// to accessing environment variables within an application. Additionally, it 
/// enhances the functionality provided by the standard <see cref="System.Environment"/>
/// class by enabling the deserialization of JSON content and the parsing of 
/// comma-delimited strings into collections.
/// 
/// Example use cases include retrieving configuration settings, parsing lists of values
/// for application initialization, and dynamically loading JSON configuration data.
/// </remarks>
public class EnvironmentWrapper : IEnvironment
{
    /// <summary>
    /// Sets an environment variable to the specified value.
    /// <code>
    /// var wrapper = new EnvironmentWrapper();
    /// wrapper.SetEnvironmentVariable("MY_VAR", "SomeValue");
    /// </code>
    /// </summary>
    /// <param name="name">The name of the environment variable.</param>
    /// <param name="value">The value to set for the environment variable.</param>
    public void SetEnvironmentVariable(string name, string value)
    {
        // Validate the input to ensure the variable name is not null or empty
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Variable name cannot be null or empty.", nameof(name));

        // Set the environment variable
        Environment.SetEnvironmentVariable(name, value);
    }

    /// <summary>
    /// Gets the value of an environment variable.
    /// <code>
    /// var wrapper = new EnvironmentWrapper();
    /// var value = wrapper.GetEnvironmentVariable("MY_VAR");
    /// </code>
    /// </summary>
    /// <param name="name">The name of the environment variable.</param>
    /// <returns>The value of the environment variable or null if it is not set.</returns>
    public string? GetEnvironmentVariable(string name)
    {
        // Validate the input to ensure the variable name is not null or empty
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Variable name cannot be null or empty.", nameof(name));

        // Retrieve and return the environment variable value
        return Environment.GetEnvironmentVariable(name);
    }

    /// <summary>
    /// Converts the value of an environment variable to a specified type.
    /// <code>
    /// var wrapper = new EnvironmentWrapper();
    /// int? port = wrapper.GetEnvironmentVariable&lt;int&gt;("PORT_NUMBER");
    /// </code>
    /// </summary>
    /// <typeparam name="T">The type to convert the value to.</typeparam>
    /// <param name="name">The name of the environment variable.</param>
    /// <returns>The converted value, or null if the conversion is not possible.</returns>
    public T? GetEnvironmentVariable<T>(string name) where T : struct
    {
        // Retrieve the environment variable value
        var value = Environment.GetEnvironmentVariable(name);

        // Return null if the value is null or empty
        if (string.IsNullOrEmpty(value))
            return null;

        // Use a type converter to convert the value to the specified type
        var converter = TypeDescriptor.GetConverter(typeof(T));
        if (converter != null && converter.IsValid(value))
            return (T)converter.ConvertFromString(value)!;

        // Return null if conversion is not possible
        return null;
    }

    /// <summary>
    /// Gets the value of an environment variable, throwing an exception if not set.
    /// <code>
    /// var wrapper = new EnvironmentWrapper();
    /// var value = wrapper.GetRequiredEnvironmentVariable("MY_VAR");
    /// </code>
    /// </summary>
    /// <param name="name">The name of the environment variable.</param>
    /// <returns>The value of the environment variable.</returns>
    public string GetRequiredEnvironmentVariable(string name)
    {
        // Validate the input to ensure the variable name is not null or empty
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Variable name cannot be null or empty.", nameof(name));

        // Retrieve the environment variable value
        var value = Environment.GetEnvironmentVariable(name);

        // Throw an exception if the value is null or empty
        if (string.IsNullOrEmpty(value))
            throw new InvalidOperationException($"Environment variable '{name}' is not set.");

        // Return the environment variable value
        return value;
    }

    /// <summary>
    /// Converts the value of an environment variable to a specified type, throwing an exception if not set.
    /// <code>
    /// var wrapper = new EnvironmentWrapper();
    /// int port = wrapper.GetRequiredEnvironmentVariable&lt;int&gt;("PORT_NUMBER");
    /// </code>
    /// </summary>
    /// <typeparam name="T">The type to convert the value to.</typeparam>
    /// <param name="name">The name of the environment variable.</param>
    /// <returns>The converted value.</returns>
    public T GetRequiredEnvironmentVariable<T>(string name) where T : struct
    {
        // Retrieve the environment variable value
        var value = Environment.GetEnvironmentVariable(name);

        // Throw an exception if the value is null or empty
        if (string.IsNullOrEmpty(value))
            throw new InvalidOperationException($"Environment variable '{name}' is not set.");

        // Use a type converter to convert the value to the specified type
        var converter = TypeDescriptor.GetConverter(typeof(T));
        if (converter != null && converter.IsValid(value))
            return (T)converter.ConvertFromString(value)!;

        // Throw an exception if conversion is not possible
        throw new InvalidOperationException($"Environment variable '{name}' cannot be converted to type {typeof(T).Name}.");
    }

    /// <summary>
    /// Parses a comma-delimited environment variable into a collection of a specified type.
    /// <code>
    /// var wrapper = new EnvironmentWrapper();
    /// List&lt;int&gt; values = wrapper.GetEnvironmentVariableCommaDelimitedValuesAsCollection&lt;List&lt;int&gt;, int&gt;("NUMBERS");
    /// </code>
    /// </summary>
    /// <typeparam name="TCollection">The type of collection to return.</typeparam>
    /// <typeparam name="TElement">The type of elements in the collection.</typeparam>
    /// <param name="name">The name of the environment variable.</param>
    /// <returns>A collection of the specified type containing elements parsed from the environment variable.</returns>
    public TCollection GetEnvironmentVariableCommaDelimitedValuesAsCollection<TCollection, TElement>(string name)
        where TCollection : ICollection<TElement>, new()
        where TElement : notnull
    {
        // Retrieve the environment variable value
        var value = Environment.GetEnvironmentVariable(name);

        // Return a new collection if the value is null or empty
        if (string.IsNullOrEmpty(value))
            return new TCollection();

        // Use a type converter to convert each item in the comma-delimited list
        var converter = TypeDescriptor.GetConverter(typeof(TElement));
        if (converter == null || !value.Split(',').Any(item => converter.IsValid(item.Trim())))
            throw new InvalidOperationException($"Unable to convert items to type {typeof(TElement).Name}.");

        // Initialize a new collection of the specified type
        var collection = new TCollection();

        // Convert each item and add to the collection
        foreach (var item in value.Split(',').Select(item => item.Trim()))
        {
            if (!string.IsNullOrEmpty(item) && converter.IsValid(item))
                collection.Add((TElement)converter.ConvertFromString(item)!);
        }

        // Return the populated collection
        return collection;
    }

    /// <summary>
    /// Deserializes a JSON string from an environment variable into a specified type.
    /// <code>
    /// var wrapper = new EnvironmentWrapper();
    /// var config = wrapper.GetEnvironmentVariableJsonAsType&lt;Config&gt;("CONFIG_JSON");
    /// </code>
    /// </summary>
    /// <typeparam name="T">The type to deserialize the JSON into.</typeparam>
    /// <param name="name">The name of the environment variable containing the JSON string.</param>
    /// <returns>The deserialized object of type T.</returns>
    public T GetEnvironmentVariableJsonAsType<T>(string name) where T : class
    {
        // Retrieve the JSON string from the environment variable
        var jsonValue = Environment.GetEnvironmentVariable(name);

        // Throw an exception if the JSON string is null or empty
        if (string.IsNullOrEmpty(jsonValue))
            throw new InvalidOperationException($"Environment variable '{name}' is not set or is empty.");

        // Deserialize the JSON string into the specified type
        return JsonConvert.DeserializeObject<T>(jsonValue)
            ?? throw new InvalidOperationException($"Deserialization of '{name}' returned null.");
    }
}
