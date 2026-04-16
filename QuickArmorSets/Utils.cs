using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace QuickArmorSets
{
    internal static class Utils
    {
        public static IEnumerator LogHierarchyDelayed(Transform transform, string title = null, float delay = 0.1f)
        {
            yield return new WaitForSeconds(delay);
            LogHierarchy(transform, title);
        }

        [Conditional("DEBUG")]
        public static void LogHierarchy(Transform transform, string title = null, string indent = "")
        {
            if (transform == null)
                return;

            if (!string.IsNullOrEmpty(title))
                Plugin.LogDebug($"{nameof(VQuickUseWheelUIPatch)}.{nameof(LogHierarchy)} | {title}");

            var componentStrings = new List<string>();
            foreach (Component component in transform.GetComponents<Component>())
            {
                if (component == null)
                    continue;

                string spriteName = string.Empty;
                bool disabled = false;
                bool hidden = false;
                switch (component)
                {
                    case Image image:
                        if (image.sprite != null)
                            spriteName = $"{image.sprite.name} [{image.color.r}, {image.color.g}, {image.color.b}, {image.color.a}]";
                        else
                            spriteName = "NULL";

                        if (!image.enabled)
                            disabled = true;
                        break;
                    case CanvasGroup canvasGroup:
                        if (canvasGroup.alpha <= 0)
                            hidden = true;
                        break;
                    case Behaviour behaviour:
                        if (!behaviour.enabled)
                            disabled = true;
                        break;
                    default:
                        break;
                }

                List<string> states = [];
                if (!component.gameObject.activeSelf)
                    states.Add("INACTIVE");
                if (disabled)
                    states.Add("DISABLED");
                if (hidden)
                    states.Add("HIDDEN");

                string componentString = component.GetType().Name;
                if (!string.IsNullOrEmpty(spriteName))
                    componentString += $" ({spriteName})";
                if (states.Any())
                    componentString += $" [{string.Join(", ", states)}]";

                componentStrings.Add(componentString);
            }

            string inactiveString = !transform.gameObject.activeSelf ? " [INACTIVE]" : string.Empty;
            Plugin.LogDebug($"{nameof(VQuickUseWheelUIPatch)}.{nameof(LogHierarchy)} | {indent}{transform.name}{inactiveString} | Components: {string.Join(", ", componentStrings)}");

            foreach (Transform child in transform)
                LogHierarchy(child, indent: indent + "  ");
        }

        public static void CopySerializedFields<T>(T source, T dest, string[] fieldsToIgnore = null, Type stopBeforeType = null)
        {
            if (source == null)
            {
                Plugin.LogWarning($"{nameof(Utils)}.{nameof(CopySerializedFields)} | {nameof(source)} is null.");
                return;
            }

            if (dest == null)
            {
                Plugin.LogWarning($"{nameof(Utils)}.{nameof(CopySerializedFields)} | {nameof(dest)} is null.");
                return;
            }

            if (stopBeforeType == null)
                stopBeforeType = typeof(object);

            Type current = typeof(T);
            while (current != null && current != stopBeforeType)
            {
                FieldInfo[] fields = current.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                foreach (FieldInfo field in fields)
                    if (field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
                    {
                        if (fieldsToIgnore != null && fieldsToIgnore.Contains(field.Name))
                            continue;

                        object sourceValue = field.GetValue(source);
                        object destValue = field.GetValue(dest);

                        string message;
                        if (sourceValue == null && destValue == null)
                            message = "are both null.";
                        else if (sourceValue == null)
                            message = $"are different. {nameof(source)} is null, {nameof(dest)}: {destValue}";
                        else if (destValue == null)
                            message = $"are different. {nameof(dest)} is null, {nameof(source)}: {sourceValue}";
                        else if (!Equals(sourceValue, destValue))
                            message = $"are different. {nameof(source)}: {sourceValue}, {nameof(dest)}: {destValue})";
                        else
                            message = "are equal.";

                        Plugin.LogDebug($"{nameof(Utils)}.{nameof(CopySerializedFields)} | {current.Name}.{field.Name} {message}");

                        field.SetValue(dest, sourceValue);
                    }

                current = current.BaseType;
            }
        }
    }
}
