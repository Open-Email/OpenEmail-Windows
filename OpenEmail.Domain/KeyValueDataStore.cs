using System.Globalization;
using CommunityToolkit.Diagnostics;

namespace OpenEmail.Domain
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class KeyValueDataStore
    {
        private string DebuggerDisplay => ToString();
        public string DataStoreInput { get; }

        protected Dictionary<string, string> Data { get; private set; } = [];

        // For download.
        public KeyValueDataStore(string dataStoreInput, int maximumInputSize = 64000) : this(maximumInputSize)
        {
            DataStoreInput = dataStoreInput;

            Guard.HasSizeLessThanOrEqualTo(DataStoreInput, maximumInputSize, nameof(DataStoreInput));

            ParseData();
        }

        // For upload.
        public KeyValueDataStore(int maximumInputSize = 64000) { }

        public void Add(string key, string value)
        {
            Data[key] = value;
        }

        /// <summary>
        /// Parses given text input and stores it in the data store.
        /// Format is: key: value
        /// </summary>
        private void ParseData()
        {
            var lines = DataStoreInput.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                // Skip lines that do not contain a colon
                if (!line.Contains(':'))
                    continue;

                var splitIndex = line.IndexOf(':');
                var key = line.Substring(0, splitIndex).Trim();
                var value = line.Substring(splitIndex + 1).Trim();

                Data[key] = value;
            }
        }

        /// <summary>
        /// Parses a single line of header and returns a dictionary of key-value pairs.
        /// Format is: key1=value1; key2=value2; key3=value3
        /// </summary>
        /// <param name="header">Header line to parse attributes for.</param>
        public static Dictionary<string, string> ParseAttributes(string header)
        {
            if (string.IsNullOrEmpty(header)) return new Dictionary<string, string>();

            var result = new Dictionary<string, string>();
            var attributes = header.Split(';');

            foreach (var attribute in attributes)
            {
                var kv = attribute.Trim().Split(['='], 2);
                if (kv.Length != 2)
                {
                    // Log error for bad header format
                    Console.Error.WriteLine("Bad header format: " + header);
                    continue;
                }

                var key = kv[0].Trim().ToLower();
                var value = kv[1].Trim();
                result[key] = value;
            }

            return result;
        }

        /// <summary>
        /// Returns a value out of key with the given type.
        /// </summary>
        /// <typeparam name="TReturnType">Expected return type.</typeparam>
        /// <param name="key">Key</param>
        /// <returns>Data with the given return type for the given key if exists. Default if not found.</returns>
        public TReturnType GetData<TReturnType>(string key, TReturnType defaultValue = default)
        {
            if (Data.TryGetValue(key, out string value)) return ConvertValue<TReturnType>(value);

            // Data not found, return default value.
            return defaultValue;
        }

        /// <summary>
        /// Converts the given value to the given type.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <param name="value">String input</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">When the requested type is not supported.</exception>
        /// <exception cref="InvalidCastException">Something went wrong with the conversion.</exception>
        private T ConvertValue<T>(string value)
        {
            try
            {
                if (string.IsNullOrEmpty(value) && IsNullableType<T>())
                {
                    return default; // Return null for nullable types if the value is empty
                }

                Type underlyingType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

                if (underlyingType == typeof(int))
                {
                    return (T)(object)int.Parse(value, CultureInfo.InvariantCulture);
                }
                else if (underlyingType == typeof(DateTime))
                {
                    return (T)(object)DateTime.Parse(value, CultureInfo.InvariantCulture);
                }
                else if (underlyingType == typeof(DateTimeOffset))
                {
                    return (T)(object)DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
                }
                else if (underlyingType == typeof(string))
                {
                    return (T)(object)value;
                }
                else if (underlyingType == typeof(bool))
                {
                    if (bool.TryParse(value, out bool booleanResult)) return (T)(object)booleanResult;

                    // Yes/No requires special handling.
                    if (value == "Yes") return (T)(object)true;
                    if (value == "No") return (T)(object)false;
                }
                else if (underlyingType == typeof(long))
                {
                    return (T)(object)long.Parse(value, CultureInfo.InvariantCulture);
                }

                throw new InvalidOperationException($"Conversion to type '{typeof(T)}' is not supported.");
            }
            catch (FormatException ex)
            {
                throw new InvalidCastException($"Failed to convert '{value}' to {typeof(T).Name}.", ex);
            }
        }

        private bool IsNullableType<T>()
        {
            return Nullable.GetUnderlyingType(typeof(T)) != null;
        }

        /// <summary>
        /// Gets the value of the key if it exists.
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Whether the value exist in the data store or not.</returns>
        public bool HasKey(string key) => Data.ContainsKey(key);

        public override string ToString()
        {
            var str = Data.Select(a => $"{a.Key}: {a.Value}\n");

            return string.Join("", str);
        }

        public void OrderKeys()
        {
            Data = Data.OrderBy(a => a.Key).ToDictionary(a => a.Key, a => a.Value);
        }
    }
}
