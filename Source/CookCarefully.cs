// Decompiled with JetBrains decompiler
// Type: CookCarefully.CookCarefully
// Assembly: CookCarefully, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7E760DD7-02E5-4645-8CBA-2B279FC6D83A
// Assembly location: C:\Users\louiz\Downloads\CookCarefully.dll

using CookCarefully.Utilities;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;

namespace CookCarefully
{
    [StaticConstructorOnStartup]
    internal static class CookCarefully
    {
        public static string DOMAIN_NAME = "com.theonly8z.cookcarefully";
        private static MethodInfo BillDoneInfo = AccessTools.Method(typeof(RecordsUtility), "Notify_BillDone", (System.Type[])null, (System.Type[])null);
        private static int IngredientsIndex = 6;

        static CookCarefully()
        {
            new Harmony(CookCarefully.DOMAIN_NAME).PatchGeneratedMethod(typeof(Toils_Recipe), (Func<MethodInfo, bool>)(m => m.Name.Contains("FinishRecipeAndStartStoringProduct")), transpiler: new HarmonyMethod(typeof(CookCarefully), "Toils_Recipe_Transpiler", (System.Type[])null));
        }

        public static IEnumerable<CodeInstruction> Toils_Recipe_Transpiler(
          IEnumerable<CodeInstruction> instructions,
          ILGenerator il)
        {
            Label skipReturn = il.DefineLabel();
            foreach (CodeInstruction i in instructions)
            {
                yield return i;
                if (CodeInstructionExtensions.Calls(i, CookCarefully.BillDoneInfo))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_0, (object)null);
                    yield return new CodeInstruction(OpCodes.Ldloc_1, (object)null);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, (object)CookCarefully.IngredientsIndex);
                    yield return new CodeInstruction(OpCodes.Call, (object)AccessTools.Method(typeof(CookCarefully), "RemoveIfPoisoned", (System.Type[])null, (System.Type[])null));
                    yield return new CodeInstruction(OpCodes.Brfalse, (object)skipReturn);
                    yield return new CodeInstruction(OpCodes.Ret, (object)null);
                    CodeInstruction skipTarget = new CodeInstruction(OpCodes.Nop, (object)null);
                    skipTarget.labels.Add(skipReturn);
                    yield return skipTarget;
                    skipTarget = (CodeInstruction)null;
                }
            }
        }

        public static bool RemoveIfPoisoned(Pawn actor, Job job, IEnumerable<Thing> things)
        {
            if (job.RecipeDef.ToString() != "CookMealCarefully")
                return false;
            foreach (Thing thing in things)
            {
                CompFoodPoisonable comp = thing.TryGetComp<CompFoodPoisonable>();
                if (comp != null && (double)comp.PoisonPercent > 0.0)
                {
                    LookTargets lookTargets = new LookTargets((Thing)actor);
                    Messages.Message(actor.Name?.ToString() + " discarded a meal because it was poisoned.", lookTargets, MessageTypeDefOf.SilentInput);
                    thing.Destroy();
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                    return true;
                }
            }
            return false;
        }
    }
}