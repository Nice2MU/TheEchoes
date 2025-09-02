using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

[CustomPropertyDrawer(typeof(RebindActionEntry))]
public class RebindIndexManager : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty actionRefProp = property.FindPropertyRelative("actionRef");
        SerializedProperty bindingIndexProp = property.FindPropertyRelative("bindingIndex");

        EditorGUI.BeginProperty(position, label, property);

        Rect actionRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(actionRect, actionRefProp);

        Rect bindingRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width, EditorGUIUtility.singleLineHeight);

        InputActionReference actionRef = actionRefProp.objectReferenceValue as InputActionReference;

        if (actionRef != null && actionRef.action != null)
        {
            var bindings = actionRef.action.bindings;
            string[] bindingNames = new string[bindings.Count];

            for (int i = 0; i < bindings.Count; i++)
            {
                bindingNames[i] = InputControlPath.ToHumanReadableString(
                    bindings[i].effectivePath,
                    InputControlPath.HumanReadableStringOptions.OmitDevice);
            }

            int currentIndex = bindingIndexProp.intValue;
            int selectedIndex = EditorGUI.Popup(bindingRect, "Binding Index", currentIndex, bindingNames);

            if (selectedIndex != currentIndex)
            {
                bindingIndexProp.intValue = selectedIndex;
            }
        }

        else
        {
            EditorGUI.LabelField(bindingRect, "Binding Index", "Select an InputActionReference first");
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 2 + 4;
    }
}