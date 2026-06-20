using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using UnityEngine;

public static class CloudDataService
{
    private const string AccountKey = "account_data";

    // Throws on failure instead of returning an empty account — callers must not
    // treat a failed load the same as a genuinely new account, or a later save
    // will overwrite the real cloud data with an empty character list.
    public static async Task<AccountData> LoadAccountAsync()
    {
        var keys = new HashSet<string> { AccountKey };
        var data = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

        if (data.TryGetValue(AccountKey, out var item))
        {
            var account = JsonUtility.FromJson<AccountData>(item.Value.GetAs<string>());
            // JsonUtility serialises null array entries as default objects — restore them to null.
            // A slot is empty only when BOTH characterId and characterName are blank
            // (old characters may have no characterId but still have a valid name).
            for (int i = 0; i < account.characters.Length; i++)
            {
                var c = account.characters[i];
                if (c != null && string.IsNullOrEmpty(c.characterId) && string.IsNullOrEmpty(c.characterName))
                    account.characters[i] = null;
            }
            return account;
        }

        // No key in the cloud at all — this really is a brand-new account.
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
