using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BattleArena.Editor.ContentBuilder
{
    /// <summary>
    /// Fluent helper that writes private [SerializeField] values the same way the Inspector does,
    /// so runtime classes can keep their fields encapsulated.
    /// </summary>
    internal readonly struct SerializedSetter
    {
        private readonly SerializedObject _serializedObject;

        private SerializedSetter(Object target) => _serializedObject = new SerializedObject(target);

        public static SerializedSetter For(Object target) => new(target);

        public SerializedSetter Set(string propertyPath, object value)
        {
            var property = _serializedObject.FindProperty(propertyPath)
                           ?? throw new ArgumentException($"{_serializedObject.targetObject.GetType().Name} has no serialized property '{propertyPath}'.");

            switch (value)
            {
                case null:
                    property.objectReferenceValue = null;
                    break;
                case Object[] objects:
                    property.arraySize = objects.Length;
                    for (var i = 0; i < objects.Length; i++)
                        property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
                    break;
                case Object obj:
                    property.objectReferenceValue = obj;
                    break;
                case float f:
                    property.floatValue = f;
                    break;
                case int i:
                    property.intValue = i;
                    break;
                case bool b:
                    property.boolValue = b;
                    break;
                case string s:
                    property.stringValue = s;
                    break;
                case Color c:
                    property.colorValue = c;
                    break;
                case Vector3 v:
                    property.vector3Value = v;
                    break;
                default:
                    throw new ArgumentException($"Unsupported value type {value.GetType().Name} for '{propertyPath}'.");
            }

            return this;
        }

        public void Apply() => _serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}
