using UnityEngine;
using UnityEngine.InputSystem;

public static class InputPersistenceManager
{
    public static void SaveRebind(InputAction action)
    {
        string rebinds = action.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("rebind_" + action.id, rebinds);
        PlayerPrefs.Save();
    }

    public static void LoadRebind(InputAction action)
    {
        string rebinds = PlayerPrefs.GetString("rebind_" + action.id, string.Empty);
        if (!string.IsNullOrEmpty(rebinds))
        {
            action.LoadBindingOverridesFromJson(rebinds);
        }
    }
}
