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

            harmony.PatchGeneratedMethod(typeof(Toils_Recipe),
                m => m.Name.Contains("FinishRecipeAndStartStoringProduct"),
                transpiler: new HarmonyMethod(typeof(CookCarefully), nameof(CookCarefully.Toils_Recipe_Transpiler)));

        }

        static MethodInfo BillDoneInfo = AccessTools.Method(typeof(RecordsUtility), nameof(RecordsUtility.Notify_BillDone));

        // 1.4 shuffled some variables around
#if v13
        static int IngredientsIndex = 5;
#elif v14
        static int IngredientsIndex = 6;
#endif

        // Patch compiler generated method for 'initAction =' delegate
        public static IEnumerable<CodeInstruction> Toils_Recipe_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            Label skipReturn = il.DefineLabel();
            foreach (CodeInstruction i in instructions)
            {
                yield return i;
                if (i.Calls(BillDoneInfo))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_0); // Pawn actor = toil.actor;
                    yield return new CodeInstruction(OpCodes.Ldloc_1); // Job curJob = actor.jobs.curJob;

                    // 1.3: List<Thing> list = GenRecipe.MakeRecipeProducts(curJob.RecipeDef, actor, ingredients, dominantIngredient, jobDriver_DoBill.BillGiver, curJob.bill.precept).ToList();
                    // 1.4: List<Thing> list = ((curJob.bill is Bill_Mech bill) ? GenRecipe.FinalizeGestatedPawns(bill, actor, style).ToList()
                    // : GenRecipe.MakeRecipeProducts(curJob.RecipeDef, actor, ingredients, dominantIngredient, jobDriver_DoBill.BillGiver, curJob.bill.precept, style, curJob.bill.graphicIndexOverride).ToList());
                    // yes it is that long
                    yield return new CodeInstruction(OpCodes.Ldloc_S, IngredientsIndex);

                    // Call our function
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CookCarefully), nameof(RemoveIfPoisoned)));

                    // This block returns when the return value of RemoveIsPoisoned is true
                    yield return new CodeInstruction(OpCodes.Brfalse, skipReturn);
                    yield return new CodeInstruction(OpCodes.Ret); // If did not skip (value was true), return early
                    var skipTarget = new CodeInstruction(OpCodes.Nop);
                    skipTarget.labels.Add(skipReturn);
                    yield return skipTarget; // skip to here if false
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
