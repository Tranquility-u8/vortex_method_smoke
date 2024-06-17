using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUSmoke
{

    public class SingleLayerAttribute : PropertyAttribute
    {
#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(SingleLayerAttribute))]
        public class SingleLayerAttributeDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                using (new EditorGUI.PropertyScope(position, label, property))
                {
                    position = EditorGUI.PrefixLabel(position, label);

                    if (property.propertyType != SerializedPropertyType.Integer)
                    {
                        EditorGUI.HelpBox(position, $"{nameof(SingleLayerAttribute)} can only be used on fields of type int!", MessageType.Error);
                    }
                    else
                    {
                        property.intValue = EditorGUI.LayerField(position, property.intValue);
                    }
                }
            }
        }
#endif
    }

}