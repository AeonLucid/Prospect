namespace Prospect.Server.Api.Models.Client
{
    public class FUpdateUserDataResult
    {
        /// <summary>
        ///     Indicates the current version of the data that has been set. This is incremented with every set call for that type of
        ///     data (read-only, internal, etc). This version can be provided in Get calls to find updated data.
        /// </summary>
        public uint DataVersion { get; set; }       
    }
}