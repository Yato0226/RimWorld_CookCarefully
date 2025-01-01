// Decompiled with JetBrains decompiler
// Type: CookCarefully.Utilities.PatchCompilerGenerated
// Assembly: CookCarefully, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7E760DD7-02E5-4645-8CBA-2B279FC6D83A
// Assembly location: C:\Users\louiz\Downloads\CookCarefully.dll

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace CookCarefully.Utilities
{
    internal static class PatchCompilerGenerated
    {
        public static void PatchGeneratedMethod(
          this Harmony harmony,
          System.Type masterType,
          Func<MethodInfo, bool> check,
          HarmonyMethod prefix = null,
          HarmonyMethod postfix = null,
          HarmonyMethod transpiler = null)
        {
            List<System.Type> list = new List<System.Type>((IEnumerable<System.Type>)masterType.GetNestedTypes(BindingFlags.NonPublic));
            while (list.Any<System.Type>())
            {
                System.Type type = list.Pop<System.Type>();
                list.AddRange((IEnumerable<System.Type>)type.GetNestedTypes(BindingFlags.NonPublic));
                foreach (MethodInfo methodInfo in AccessTools.GetDeclaredMethods(type).Where<MethodInfo>(check))
                    harmony.Patch((MethodBase)methodInfo, prefix, postfix, transpiler, (HarmonyMethod)null);
            }
        }
    }
}
