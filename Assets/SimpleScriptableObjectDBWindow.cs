using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Collections;
using UnityEditor.AnimatedValues;

public class SimpleScriptableObjectDBWindow : EditorWindow
{
    private class ItemsWidth
    {
        public int numberWidth;
        public int numberRangedWidth;
        public int boolWidth;
        public int stringWidth;
        public int enumWidth;
        public int colorWidth;
        public int vector3Width;
        public int vector2Width;
        public int referenceWidth;

        public ItemsWidth()
        {
            numberWidth = 100;
            numberRangedWidth = 200;
            boolWidth = 100;
            stringWidth = 200;
            enumWidth = 100;
            vector3Width = 300;
            vector2Width = 200;
            referenceWidth = 200;
            colorWidth = 200;
        }
    }
    static Color verticalLinesColor;
    const int height = 20;
    static ItemsWidth itemsWidth;

    static List<ScriptableObject> itemList;

    private static AnimBool bUseFilters;
    private static List<object> filters;
    private static List<AnimBool> boolfilters;
    private static string filterString;

    [SerializeField]
    public static Type scriptableObjectToVisualize;

    static Vector2 MainScrollPos;
    static Vector2 InternalScrollPos;

    static HashSet<ScriptableObject> itemsToRemove;
    static HashSet<ScriptableObject> oldItemsToRemove;

    protected static int maxItemsPerPage = 20;
    private static int currentPage = 0;
    private static int totalFilteredItems = 0;

    private static GUIStyle ErrorStyle;

    protected static void OverrideWidths(int numberWidth, int numberRangedWidth, int boolWidth, int enumWidth, int vector3Width, int vector2Width, int colorWidth, int referenceWidth)
    {
        itemsWidth.numberWidth = numberWidth;
        itemsWidth.numberRangedWidth = numberRangedWidth;
        itemsWidth.boolWidth = boolWidth;
        itemsWidth.enumWidth = enumWidth;
        itemsWidth.vector3Width = vector3Width;
        itemsWidth.vector2Width = vector2Width;
        itemsWidth.colorWidth = colorWidth;
        itemsWidth.referenceWidth = referenceWidth;
    }

    private static void PrepareFilterList()
    {
        bUseFilters = new AnimBool(false);
        filters = new List<object>();
        boolfilters = new List<AnimBool>();

        FieldInfo[] allFields = scriptableObjectToVisualize.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        for (int i = 0; i < allFields.Length; i++)
        {
            FieldInfo field = allFields[i];
            if (field.FieldType == typeof(bool) || field.FieldType == typeof(int) || field.FieldType == typeof(float) || field.FieldType.IsEnum)
            {
                AnimBool animBool = new AnimBool(false);
                boolfilters.Add(animBool);
            }
            if (field.FieldType == typeof(bool))
            {
                filters.Add(false);
            }
            if (field.FieldType == typeof(int))
            {
                var range = field.GetCustomAttribute<RangeAttribute>();
                if (range!= null)
                {
                    filters.Add((int)range.min);
                    filters.Add((int)range.max);
                }
                else
                {
                    filters.Add((int)int.MinValue);
                    filters.Add((int)int.MaxValue);
                }
            }
            if (field.FieldType == typeof(float))
            {
                var range = field.GetCustomAttribute<RangeAttribute>();
                if (range != null)
                {
                    filters.Add((float)range.min);
                    filters.Add((float)range.max);
                }
                else
                {
                    filters.Add((float)float.MinValue);
                    filters.Add((float)float.MaxValue);
                }
            }      
            if (field.FieldType.IsEnum)
            {
                filters.Add((int)-1);
            }           
        }
        
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ScriptableObjectType">The ScriptableObjectType for which generate the window</param>
    /// <param name="itemsPerPage">Items in list per page, if <=0, there will be no limit </param>
    protected static void Setup(Type ScriptableObjectType, int itemsPerPage = 20)
    {
        maxItemsPerPage = itemsPerPage;
        verticalLinesColor = new Color(0.3f, 0.3f, 0.3f, 1);
        filterString = "";

        itemsWidth = new ItemsWidth();

        scriptableObjectToVisualize = ScriptableObjectType;
        ScriptableObject[] cards = Resources.LoadAll<ScriptableObject>("");
        itemList = new List<ScriptableObject>();

        PrepareFilterList();

        foreach (ScriptableObject o in cards)
        {
            if (o.GetType() == scriptableObjectToVisualize)
            {
                itemList.Add(o);
            }
        }

        EditorWindow.GetWindow(typeof(SimpleScriptableObjectDBWindow));
    }

    private void FilterItems()
    {
        FieldInfo[] allFields = scriptableObjectToVisualize.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);
        // currentIndex and currentAnimIndex are used to track fileterlist current index
        int currentIndex = 0;
        int currentAnimIndex = 0;

        foreach (FieldInfo field in allFields)
        {
            string fieldName = char.ToUpper(field.Name[0]) + field.Name.Substring(1);
            if (field.FieldType == typeof(bool))
            {
                boolfilters[currentAnimIndex].target = EditorGUILayout.ToggleLeft("Filter by " + fieldName, boolfilters[currentAnimIndex].target);
                if (EditorGUILayout.BeginFadeGroup(boolfilters[currentAnimIndex].faded))
                {
                    EditorGUI.indentLevel++;
                    filters[currentIndex] = (bool)EditorGUILayout.ToggleLeft("Value", (bool)(filters[currentIndex]), GUILayout.Width(200));
                    foreach (ScriptableObject s in itemList)
                    {
                        if ((bool)field.GetValue(s) != (bool)(filters[currentIndex]))
                        {
                            itemsToRemove.Add(s);
                        }
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFadeGroup();
                currentIndex++;
                currentAnimIndex++;

            }
            else if (field.FieldType == typeof(int))
            {
                boolfilters[currentAnimIndex].target = EditorGUILayout.ToggleLeft("Filter by " + fieldName, boolfilters[currentAnimIndex].target);
                if (EditorGUILayout.BeginFadeGroup(boolfilters[currentAnimIndex].faded))
                {
                    EditorGUI.indentLevel++;
                    var range = field.GetCustomAttribute<RangeAttribute>();
                    if (range != null)
                    {
                        filters[currentIndex] = EditorGUILayout.IntSlider("Min", (int)(filters[currentIndex]), (int)range.min, (int)range.max, GUILayout.Width(itemsWidth.numberRangedWidth + 200), GUILayout.Height(height));
                        filters[currentIndex + 1] = EditorGUILayout.IntSlider("Max", (int)(filters[currentIndex + 1]), (int)range.min, (int)range.max, GUILayout.Width(itemsWidth.numberRangedWidth + 200), GUILayout.Height(height));
                    }
                    else
                    {
                        filters[currentIndex] = EditorGUILayout.IntField("Min", (int)(filters[currentIndex]), GUILayout.Width(itemsWidth.numberWidth + 200), GUILayout.Height(height));
                        filters[currentIndex + 1] = EditorGUILayout.IntField("Max", (int)(filters[currentIndex + 1]), GUILayout.Width(itemsWidth.numberWidth + 200), GUILayout.Height(height));
                    }
                    foreach (ScriptableObject s in itemList)
                    {
                        if ((int)field.GetValue(s) < (int)filters[currentIndex] || (int)field.GetValue(s) > (int)filters[currentIndex + 1])
                        {
                            itemsToRemove.Add(s);
                        }
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFadeGroup();
                currentIndex++;
                currentIndex++;
                currentAnimIndex++;
            }
            else if (field.FieldType == typeof(float))
            {
                boolfilters[currentAnimIndex].target = EditorGUILayout.ToggleLeft("Filter by " + fieldName, boolfilters[currentAnimIndex].target);
                if (EditorGUILayout.BeginFadeGroup(boolfilters[currentAnimIndex].faded))
                {
                    EditorGUI.indentLevel++;
                    var range = field.GetCustomAttribute<RangeAttribute>();
                    if (range != null)
                    {
                        filters[currentIndex] = EditorGUILayout.Slider("Min", (float)(filters[currentIndex]), (float)range.min, (int)range.max, GUILayout.Width(itemsWidth.numberRangedWidth + 200), GUILayout.Height(height));
                        filters[currentIndex + 1] = EditorGUILayout.Slider("Min", (float)(filters[currentIndex + 1]), (float)range.min, (int)range.max, GUILayout.Width(itemsWidth.numberRangedWidth + 200), GUILayout.Height(height));
                    }
                    else
                    {
                        filters[currentIndex] = EditorGUILayout.FloatField("Min", (float)(filters[currentIndex]), GUILayout.Width(itemsWidth.numberWidth + 200), GUILayout.Height(height));
                        filters[currentIndex + 1] = EditorGUILayout.FloatField("Max", (float)(filters[currentIndex + 1]), GUILayout.Width(itemsWidth.numberWidth + 200), GUILayout.Height(height));
                    }

                    foreach (ScriptableObject s in itemList)
                    {
                        if ((float)field.GetValue(s) < (float)filters[currentIndex] || (float)field.GetValue(s) > (float)filters[currentIndex + 1])
                        {
                            itemsToRemove.Add(s);
                        }
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFadeGroup();
                currentIndex++;
                currentIndex++;
                currentAnimIndex++;

            }
            else if (field.FieldType.IsEnum)
            {
                boolfilters[currentAnimIndex].target = EditorGUILayout.ToggleLeft("Filter by " + fieldName, boolfilters[currentAnimIndex].target);
                if ((int)(filters[currentIndex]) == -1 && itemList.Count > 0)
                {
                    filters[currentIndex] = (Enum)field.GetValue(itemList[0]);
                }
                if (EditorGUILayout.BeginFadeGroup(boolfilters[currentAnimIndex].faded))
                {
                    EditorGUI.indentLevel++;
                    filters[currentIndex] = EditorGUILayout.EnumPopup((Enum)(filters[currentIndex]), GUILayout.Width(itemsWidth.enumWidth), GUILayout.Height(height));

                    foreach (ScriptableObject s in itemList)
                    {
                        if (!((Enum)field.GetValue(s)).Equals((Enum)(filters[currentIndex])))
                        {
                            itemsToRemove.Add(s);
                        }
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFadeGroup();
                currentIndex++;
                currentAnimIndex++;
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void FilterItemsByString()
    {
        FieldInfo[] allFields = scriptableObjectToVisualize.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        EditorGUILayout.BeginVertical();
        filterString = EditorGUILayout.TextField(new GUIContent("Search", "Search in the name and every string of the scriptable object"), filterString, GUILayout.Width(500));
        filterString = filterString.ToLower();
        if (filterString != "")
        {
            Hashtable texts = new Hashtable();
            foreach (ScriptableObject s in itemList)
            {
                if (!itemsToRemove.Contains(s))
                {
                    texts.Add(s, s.name);
                    foreach (FieldInfo field in allFields)
                    {
                        if (field.FieldType == typeof(string))
                        {
                            texts[s] = (string)texts[s] + (string)field.GetValue(s);
                        }
                    }
                }
            }
            foreach (ScriptableObject s in itemList)
            {
                if (texts.Contains(s))
                {
                    if (!((string)texts[s]).ToLower().Contains(filterString))
                    {
                        itemsToRemove.Add(s);
                    }
                }
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void Awake()
    {
        foreach (AnimBool anim in boolfilters)
        {
            anim.valueChanged.RemoveAllListeners();
            anim.valueChanged.AddListener(Repaint);
        }
        bUseFilters.valueChanged.AddListener(Repaint);

    }

    void OnGUI()
    {
        ErrorStyle = new GUIStyle(GUI.skin.label) {};
        ErrorStyle.normal.textColor = Color.red;

        oldItemsToRemove = itemsToRemove;
        itemsToRemove = new HashSet<ScriptableObject>();
        EditorGUILayout.Space(10);

        bUseFilters.target = EditorGUILayout.ToggleLeft("Use Filters", bUseFilters.target);
        if (EditorGUILayout.BeginFadeGroup(bUseFilters.faded))
        {
            FilterItems();
        }
        CheckResetPageIndex();
        EditorGUILayout.EndFadeGroup();

        FilterItemsByString();

        EditorGUILayout.Space(10);
        ExtractAndGenerateFields();

        GeneratePageList();

    }

    private void CheckResetPageIndex()
    {

        if (currentPage != 0)
        {
            foreach (ScriptableObject s in oldItemsToRemove)
            {
                if (!itemsToRemove.Contains(s))
                {
                    currentPage = 0;
                    break;
                }
            }
        }
        if (currentPage != 0)
        {
            foreach (ScriptableObject s in itemsToRemove)
            {
                if (!oldItemsToRemove.Contains(s))
                {
                    currentPage = 0;
                    break;
                }
            }
        }
    }

    private void GeneratePageList()
    {
        if (maxItemsPerPage <= 0) return;
        int totalPages = (totalFilteredItems / maxItemsPerPage) + ((totalFilteredItems % maxItemsPerPage)>0?1:0);
        EditorGUILayout.BeginHorizontal();
        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true };
        EditorGUILayout.LabelField("Page", style, GUILayout.Width(50));

        for (int i=0; i< totalPages; i++)
        {
            if (i == currentPage)
            {
                GUI.enabled = false;
                GUILayout.Button("" + i, GUILayout.Width(50));
                GUI.enabled = true;
            }
            else if (GUILayout.Button("" + i, GUILayout.Width(50)))
            {
                currentPage = i;
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(20);
    }

    private void ExtractAndGenerateSingleField(FieldInfo field, object father)
    {
        object fieldValue = field.GetValue(father);
        object newValue;
        if (field.FieldType == typeof(bool))
        {
            EditorGUI.BeginChangeCheck();
            newValue = EditorGUILayout.Toggle((bool)(fieldValue), GUILayout.Width(itemsWidth.boolWidth), GUILayout.Height(height));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject((UnityEngine.Object)father, "changed " + field.Name + " of " + father.ToString());
                field.SetValue(father, newValue);
                EditorUtility.SetDirty((UnityEngine.Object)father);
            }
        }
        else if (field.FieldType == typeof(int))
        {
            //TODO ranges if(field.CustomAttributes.Contains(RangeAttribute))            
            EditorGUI.BeginChangeCheck();
            var range = field.GetCustomAttribute<RangeAttribute>();
            if (range != null)
            {
                newValue = EditorGUILayout.IntSlider((int)(fieldValue), (int)range.min, (int)range.max, GUILayout.Width(itemsWidth.numberRangedWidth), GUILayout.Height(height));
            }
            else
            {
                newValue = EditorGUILayout.IntField((int)(fieldValue), GUILayout.Width(itemsWidth.numberWidth), GUILayout.Height(height));
            }
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject((UnityEngine.Object)father, "changed " + field.Name + " of " + father.ToString());
                field.SetValue(father, newValue);
                EditorUtility.SetDirty((UnityEngine.Object)father);
            }
        }
        else if (field.FieldType == typeof(float))
        {
            EditorGUI.BeginChangeCheck();
            var range = field.GetCustomAttribute<RangeAttribute>();
            if (range != null)
            {
                newValue = EditorGUILayout.Slider((float)(fieldValue), (float)range.min, (float)range.max, GUILayout.Width(itemsWidth.numberRangedWidth), GUILayout.Height(height));
            }
            else
            {
                newValue = EditorGUILayout.FloatField((float)(fieldValue), GUILayout.Width(itemsWidth.numberWidth), GUILayout.Height(height));
            }
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject((UnityEngine.Object)father, "changed " + field.Name + " of " + father.ToString());
                field.SetValue(father, newValue);
                EditorUtility.SetDirty((UnityEngine.Object)father);
            }
        }
        else if (field.FieldType == typeof(string))
        {
            EditorGUI.BeginChangeCheck();
            newValue = EditorGUILayout.TextField((string)(fieldValue), GUILayout.Width(itemsWidth.stringWidth), GUILayout.Height(height));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject((UnityEngine.Object)father, "changed " + field.Name + " of " + father.ToString());
                field.SetValue(father, newValue);
                EditorUtility.SetDirty((UnityEngine.Object)father);
            }
        }
        else if (field.FieldType == typeof(Color))
        {
            EditorGUI.BeginChangeCheck();
            newValue = EditorGUILayout.ColorField((Color)fieldValue, GUILayout.Width(itemsWidth.colorWidth), GUILayout.Height(height));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject((UnityEngine.Object)father, "changed " + field.Name + " of " + father.ToString());
                field.SetValue(father, newValue);
                EditorUtility.SetDirty((UnityEngine.Object)father);
            }
        }
        else if (field.FieldType.IsEnum)
        {
            EditorGUI.BeginChangeCheck();
            newValue = EditorGUILayout.EnumPopup((Enum)fieldValue, GUILayout.Width(itemsWidth.enumWidth), GUILayout.Height(height));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject((UnityEngine.Object)father, "changed " + field.Name + " of " + father.ToString());
                field.SetValue(father, newValue);
                EditorUtility.SetDirty((UnityEngine.Object)father);
            }
        }
        else if (field.FieldType == typeof(Vector3))
        {
            newValue = EditorGUILayout.Vector3Field("", (Vector3)fieldValue, GUILayout.Width(itemsWidth.vector3Width), GUILayout.Height(height));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject((UnityEngine.Object)father, "changed " + field.Name + " of " + father.ToString());
                field.SetValue(father, newValue);
                EditorUtility.SetDirty((UnityEngine.Object)father);
            }
        }
        else if (field.FieldType == typeof(Vector2))
        {
            newValue = EditorGUILayout.Vector2Field("", (Vector2)fieldValue, GUILayout.Width(itemsWidth.vector2Width), GUILayout.Height(height));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject((UnityEngine.Object)father, "changed " + field.Name + " of " + father.ToString());
                field.SetValue(father, newValue);
                EditorUtility.SetDirty((UnityEngine.Object)father);
            }
        }
        else if (field.FieldType == typeof(Vector3Int))
        {
            newValue = EditorGUILayout.Vector3IntField("", (Vector3Int)fieldValue, GUILayout.Width(itemsWidth.vector3Width), GUILayout.Height(height));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject((UnityEngine.Object)father, "changed " + field.Name + " of " + father.ToString());
                field.SetValue(father, newValue);
                EditorUtility.SetDirty((UnityEngine.Object)father);
            }
        }
        else if (field.FieldType == typeof(Vector2Int))
        {
            newValue = EditorGUILayout.Vector2IntField("", (Vector2Int)fieldValue, GUILayout.Width(itemsWidth.vector2Width), GUILayout.Height(height));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject((UnityEngine.Object)father, "changed " + field.Name + " of " + father.ToString());
                field.SetValue(father, newValue);
                EditorUtility.SetDirty((UnityEngine.Object)father);
            }
        }
        else if (field.FieldType == typeof(Sprite))
        {
            newValue = (Sprite)EditorGUILayout.ObjectField((Sprite)fieldValue, typeof(Sprite), false, GUILayout.Width(itemsWidth.referenceWidth));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject((UnityEngine.Object)father, "changed " + field.Name + " of " + father.ToString());
                field.SetValue(father, newValue);
                EditorUtility.SetDirty((UnityEngine.Object)father);
            }
        }
        else if (field.FieldType.IsArray)
        {
            EditorGUILayout.LabelField("Array are not supported", ErrorStyle, GUILayout.Width(itemsWidth.referenceWidth), GUILayout.Height(height));
        }
        else if (field.FieldType.IsValueType && !field.FieldType.IsEnum)
        {
            EditorGUILayout.LabelField("Struct are not supported", ErrorStyle, GUILayout.Width(itemsWidth.referenceWidth), GUILayout.Height(height));
        }
        else
        {
            newValue = (UnityEngine.Object)EditorGUILayout.ObjectField((UnityEngine.Object)fieldValue, field.FieldType, false, GUILayout.Width(itemsWidth.referenceWidth), GUILayout.Height(height));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject((UnityEngine.Object)father, "changed " + field.Name + " of " + father.ToString());
                field.SetValue(father, newValue);
                EditorUtility.SetDirty((UnityEngine.Object)father);
            }
        }
    }

    private void GenerateSingleFieldDescription(FieldInfo field)
    {
        int width = itemsWidth.referenceWidth;
        if (field.FieldType == typeof(int)|| field.FieldType == typeof(float))
        {
            if (field.GetCustomAttribute<RangeAttribute>() != null)
            {
                width = itemsWidth.numberRangedWidth;
            }
            else
            {
                width = itemsWidth.numberWidth;
            }
        }
        else if (field.FieldType == typeof(bool))
        {
            width = itemsWidth.boolWidth;
        }
        else if (field.FieldType == typeof(string))
        {
            width = itemsWidth.stringWidth;
        }
        else if (field.FieldType == typeof(Color))
        {
            width = itemsWidth.colorWidth;
        }
        else if (field.FieldType.IsEnum)
        {
            width = itemsWidth.enumWidth;
        }
        else if (field.FieldType == typeof(Vector3))
        {
            width = itemsWidth.vector3Width;
        }
        else if (field.FieldType == typeof(Vector2))
        {
            width = itemsWidth.vector2Width;
        }
        else if (field.FieldType == typeof(Vector3Int))
        {
            width = itemsWidth.vector3Width;
        }
        else if (field.FieldType == typeof(Vector2Int))
        {
            width = itemsWidth.vector2Width;
        }
        string fieldName = char.ToUpper(field.Name[0]) + field.Name.Substring(1);
        EditorGUILayout.LabelField(fieldName, EditorStyles.boldLabel, GUILayout.Width(width));
    }
       
    private void ExtractAndGenerateFields()
    {
        if (maxItemsPerPage > 0)
            MainScrollPos = EditorGUILayout.BeginScrollView(MainScrollPos, false, false, GUILayout.MaxHeight((maxItemsPerPage+1)*27 + 20+ 5));
        else
            MainScrollPos = EditorGUILayout.BeginScrollView(MainScrollPos, false, false);
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(scriptableObjectToVisualize.Name +  " Name", GUILayout.Width(itemsWidth.stringWidth));

        FieldInfo[] allFields = scriptableObjectToVisualize.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

        foreach (FieldInfo field in allFields)
        {
            GenerateSingleFieldDescription(field);
            GUILayout.Space(4);
            //DrawVerticalUILine(15);
        }
        GUILayout.EndHorizontal();

        DrawUILine(Color.white);

        InternalScrollPos = EditorGUILayout.BeginScrollView(InternalScrollPos, GUIStyle.none, GUIStyle.none);
        totalFilteredItems = 0;
        foreach (ScriptableObject scriptableObj in itemList)
        {
            if(itemsToRemove.Contains(scriptableObj))
            {
                continue;
            }
            totalFilteredItems++;
            if (maxItemsPerPage > 0)
            {
                if (totalFilteredItems <= maxItemsPerPage * currentPage) continue;
                if (totalFilteredItems > maxItemsPerPage * (currentPage + 1)) continue;
            }
            GUILayout.BeginHorizontal();
            //System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("\\(\\w+\\)");
            //EditorGUILayout.LabelField(regex.Replace(scriptableObj.ToString(), ""), GUILayout.Width(itemsWidth.stringWidth));
            //EditorGUILayout.LabelField(scriptableObj.name, GUILayout.Width(itemsWidth.stringWidth));

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField(scriptableObj.name, GUILayout.Width(itemsWidth.stringWidth-10), GUILayout.Height(height));           
            GUILayout.Space(10);
            //Type objType = scriptableObj.GetType();
            //FieldInfo[] allFields = objType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            foreach (FieldInfo field in allFields)
            {

                ExtractAndGenerateSingleField(field, scriptableObj);
                GUILayout.Space(4);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndScrollView();

    }

    private static void DrawUILine(Color c, int thickness = 1, int padding = 10)
    {
        Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
        r.height = thickness;
        r.y += padding / 2;
        r.x -= 2;
        r.width += 6;
        EditorGUI.DrawRect(r, c);
    }
}