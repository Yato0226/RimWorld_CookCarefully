using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

using CookCarefully.Utilities;
using System.Reflection;
using System.Reflection.Emit;

namespace CookCarefully
{
    [StaticConstructorOnStartup]
    static class CookCarefully
    {
        public static string DOMAIN_NAME = "com.theonly8z.cookcarefully";
        
        static CookCarefully()
        {
            Harmony harmony = new Harmony(DOMAIN_NAME);
            /// harmony.PatchAll();

            Harmony.DEBUG = true;

            harmony.PatchGeneratedMethod(typeof(Toils_Recipe),
                m => m.Name.Contains("FinishRecipeAndStartStoringProduct"),
                transpiler: new HarmonyMethod(typeof(CookCarefully), nameof(CookCarefully.Toils_Recipe_Transpiler)));

        }

        static MethodInfo BillDoneInfo = AccessTools.Method(typeof(RecordsUtility), nameof(RecordsUtility.Notify_BillDone));

        // Patch compiler generated method for 'initAction =' delegate
        public static IEnumerable<CodeInstruction> Toils_Recipe_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            // too early
            // MethodInfo MakeRecipeProductsInfo = AccessTools.Method(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts));

            Label skipReturn = il.DefineLabel();
            

            foreach (CodeInstruction i in instructions)
            {
                yield return i;

                if (i.Calls(BillDoneInfo))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_0); // Pawn actor = toil.actor;
                    yield return new CodeInstruction(OpCodes.Ldloc_1); // Job curJob = actor.jobs.curJob;
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 5); // List<Thing> list = GenRecipe.MakeRecipeProducts(curJob.RecipeDef, actor, ingredients, dominantIngredient, jobDriver_DoBill.BillGiver, curJob.bill.precept).ToList();
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CookCarefully), nameof(RemoveIfPoisoned)));

                    // Want to return if top of stack is true
                    yield return new CodeInstruction(OpCodes.Brfalse, skipReturn);

                    // If did not skip (balue was true), return early
                    yield return new CodeInstruction(OpCodes.Ret);

                    var skipTarget = new CodeInstruction(OpCodes.Nop);
                    skipTarget.labels.Add(skipReturn);
                    yield return skipTarget;
                }
            }
        }

        public static bool RemoveIfPoisoned(Pawn actor, Job job, IEnumerable<Thing> things)
        {
            if (job.RecipeDef.ToString() != "CookMealCarefully")
                return false;

            foreach (Thing thing in things)
            {
                CompFoodPoisonable compFoodPoisonable = thing.TryGetComp<CompFoodPoisonable>();
                if (compFoodPoisonable != null && compFoodPoisonable.PoisonPercent > 0)
                {
                    var lookTarget = new LookTargets(actor);
                    Messages.Message(actor.Name + " discarded a meal because it was poisoned.", lookTarget, MessageTypeDefOf.SilentInput);
                    thing.Destroy();
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                    return true;
                }
            }
            return false;
        }

    }

}
