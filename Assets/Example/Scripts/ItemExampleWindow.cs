using UnityEditor;

public class ItemExampleWindow : SimpleScriptableObjectDBWindow
{
    [MenuItem("Window/Items")]
    public static void ShowWindow()
    {
        //Mandatory insert your ScriptableObject class here items per page is optional
        Setup(typeof(ItemExample), 20);

        //Optional: if your scriptable object has references to other custom types, such as other scriptable objects, add them here
        customTypes.Add(typeof(CardExample));

        //Optional: you can override element widths
        OverrideWidths(100, 200, 100, 100, 300, 200, 200, 200);
    }

}