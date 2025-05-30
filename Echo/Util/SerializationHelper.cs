using System.IO;
using System.Text.Json;

namespace Echo.Util
{
    public static class SerializationHelper
    {
        private static readonly JsonSerializerOptions _defaultOptions = new JsonSerializerOptions
        {
            WriteIndented = true, // for human-readable files
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Serializes an object to JSON and writes it to the specified file path.
        /// </summary>
        public static async Task SerializeToFileAsync<T>(T obj, string filePath, JsonSerializerOptions? options = null)
        {
            options ??= _defaultOptions;
            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            await using var stream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(stream, obj, options);
        }

        /// <summary>
        /// Reads JSON from the specified file and deserializes it into an object of type T.
        /// </summary>
        public static async Task<T?> DeserializeFromFileAsync<T>(string filePath, JsonSerializerOptions? options = null)
        {
            options ??= _defaultOptions;

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            await using var stream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync<T>(stream, options);
        }

        /// <summary>
        /// Synchronous convenience wrapper for serializing to file.
        /// </summary>
        public static void SerializeToFile<T>(T obj, string filePath, JsonSerializerOptions? options = null)
        {
            var json = JsonSerializer.Serialize(obj, options ?? _defaultOptions);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Synchronous convenience wrapper for deserializing from file.
        /// </summary>
        public static T? DeserializeFromFile<T>(string filePath, JsonSerializerOptions? options = null)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<T>(json, options ?? _defaultOptions);
        }
    }

}
