using Unity.Services.Authentication;
using UnityEngine;

/// <summary>
/// Signs the player out of Unity Auth before returning to the Login screen.
/// </summary>
public static class AuthBackHelper
{
    public static void SignOutAndReturn()
    {
        try
        {
            if (AuthenticationService.Instance.IsSignedIn)
                AuthenticationService.Instance.SignOut();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[AuthBackHelper] Sign out error: " + e.Message);
        }
    }
}
