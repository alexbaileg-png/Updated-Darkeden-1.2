using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using UnityEngine;

public static class CloudDataService
{
    private const string AccountKey = "account_data";

    public static async Task<AccountData> LoadAccountAsync()
    {
        try
        {
            var keys = new HashSet<string> { AccountKey };
            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            if (data.TryGetValue(AccountKey, out var item))
                return JsonUtility.FromJson<AccountData>(item.Value.GetAs<string>());
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CloudSave] Load failed: {e.Message}");
        }

        return new AccountData();
    }

    public static async Task SaveAccountAsync(AccountData account)
    {
        try
        {
            string json = JsonUtility.ToJson(account);
            var data = new Dictionary<string, object> { { AccountKey, json } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CloudSave] Save failed: {e.Message}");
        }
    }
}
