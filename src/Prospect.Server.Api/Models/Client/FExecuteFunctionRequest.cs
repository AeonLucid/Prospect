using System.Collections.Generic;
using System.Text.Json.Serialization;
using Prospect.Server.Api.Models.Client.Data;

namespace Prospect.Server.Api.Models.Client
{
    public class FExecuteFunctionRequest
    {
        /// <summary>
        ///     [optional] The optional custom tags associated with the request (e.g. build number, external trace identifiers, etc.).
        /// </summary>
        [JsonPropertyName("CustomTags")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Dictionary<string, string> CustomTags { get; set; }
        
        /// <summary>
        ///     [optional] The entity to perform this action on.
        /// </summary>
        [JsonPropertyName("Entity")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public FEntityKey Entity { get; set; }
        
        /// <summary>
        ///     The name of the CloudScript function to execute
        /// </summary>
        [JsonPropertyName("FunctionName")]
        public string FunctionName { get; set; }
        
        /// <summary>
        ///     [optional] Object that is passed in to the function as the FunctionArgument field of the FunctionExecutionContext data structure
        /// </summary>
        [JsonPropertyName("FunctionParameter")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string FunctionParameter { get; set; }
        
        /// <summary>
        ///     [optional] Generate a 'entity_executed_cloudscript_function' PlayStream event containing the results of the function execution and
        ///     other contextual information. This event will show up in the PlayStream debugger console for the player in Game Manager.
        /// </summary>
        [JsonPropertyName("GeneratePlayStreamEvent")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool? GeneratePlayStreamEvent { get; set; }
    }
}