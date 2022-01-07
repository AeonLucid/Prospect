namespace Prospect.Server.Api.Services.CloudScript;

[AttributeUsage(AttributeTargets.Class)]
public class CloudScriptFunction : Attribute
{
    public CloudScriptFunction(string name)
    {
        Name = name;
    }
    
    public string Name { get; }
}