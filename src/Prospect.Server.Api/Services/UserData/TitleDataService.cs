namespace Prospect.Server.Api.Services.UserData;

public class TitleDataService
{
	public TitleDataService(ILogger<TitleDataService> logger)
	{
            
	}

	public Dictionary<string, string> Find(List<string> keys)
	{
		if (keys != null && keys.Count > 0)
		{
			var result = new Dictionary<string, string>();
		        
			foreach (var key in keys)
			{
				if (TitleDataDefault.Data.TryGetValue(key, out var value))
				{
					result.Add(key, value);
				}
			}
		        
			return result;
		}

		return TitleDataDefault.Data;
	}
}