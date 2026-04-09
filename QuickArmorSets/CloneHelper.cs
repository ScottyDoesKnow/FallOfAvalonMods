using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Awaken.TG.Main.AI.Combat.CustomDeath.Conditions;
using UnityEngine;

namespace QuickArmorSets
{
    internal static class CloneHelper
    {
        public static void CopySerializedFields<T>(T source, T dest, string[] fieldsToIgnore = null, Type stopBeforeType = null)
        {
            if (source == null)
            {
                Plugin.Log?.LogError($"{nameof(CloneHelper)}.{nameof(CopySerializedFields)} | {nameof(source)} is null.");
                return;
            }

            if (dest == null)
            {
                Plugin.Log?.LogError($"{nameof(CloneHelper)}.{nameof(CopySerializedFields)} | {nameof(dest)} is null.");
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

                        Plugin.Log?.LogDebug($"{nameof(CloneHelper)}.{nameof(CopySerializedFields)} | {current.Name}.{field.Name} {message}");

                        field.SetValue(dest, sourceValue);
                    }

                current = current.BaseType;
            }
        }
    }
}
