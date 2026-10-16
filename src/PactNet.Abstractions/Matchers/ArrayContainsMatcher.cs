using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PactNet.Matchers
{
    /// <summary>
    /// Matcher for array-contains. Checks whether an array contains the specified variations.
    /// </summary>
    public class ArrayContainsMatcher : IMatcher
    {
        /// <summary>
        /// Type of the matcher
        /// </summary>
        [JsonPropertyName("pact:matcher:type")]
        public string Type => "array-contains";

        /// <summary>
        /// The items expected to be in the array.
        /// </summary>
        [JsonPropertyName("variants")]
        public ICollection<dynamic> Value { get; }

        dynamic IMatcher.Value => Value;

        /// <summary>
        /// Initialises a new instance of the <see cref="ArrayContainsMatcher"/> class.
        /// </summary>
        /// <param name="variants">The variants expected to be in the array.</param>
        public ArrayContainsMatcher(ICollection<dynamic> variants)
        {
            Value = variants;
        }
    }
}
