using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace AutomaticNudistOutfit
{
    [StaticConstructorOnStartup]
    public static class AutomaticNudistOutfit
    {
        private static void AutoNudistOutfit(Pawn pawn)
        {
            if (pawn?.story?.traits?.HasTrait(TraitDefOf.Nudist) == true &&
                pawn.outfits != null &&
                !WorldComp.PawnsWithNudist.Contains(pawn))
            {
                List<Outfit> allOutfits = Current.Game.outfitDatabase.AllOutfits;

                foreach (var outfit in allOutfits)
                {
                    if (outfit.label == "Nudist")
                    {
                        pawn.outfits.CurrentOutfit = outfit;
                    }
                    WorldComp.PawnsWithNudist.Add(pawn);
                }

            }
        }

        [HarmonyPatch(typeof(Thing), nameof(Thing.SpawnSetup))]
        public static class Patch_Thing_SpawnSetup
        {
            // Patching for initial pawns
            public static void Postfix(Thing __instance)
            {
                if (__instance is Pawn p && p.Faction?.IsPlayer == true && p.def?.race?.Humanlike == true)
                {
                    AutoNudistOutfit(p);
                }
            }
        }

        [HarmonyPatch(typeof(InteractionWorker_RecruitAttempt), nameof(InteractionWorker_RecruitAttempt.DoRecruit), new Type[] { typeof(Pawn), typeof(Pawn), typeof(string), typeof(string), typeof(bool), typeof(bool) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out, ArgumentType.Normal, ArgumentType.Normal })]
        public static class Patch_InteractionWorker_RecruitAttempt
        {
            // Patching for recruited prisoners
            public static void Postfix(Pawn recruiter, Pawn recruitee)
            {
                if (recruitee is Pawn p && p.Faction?.IsPlayer == true && p.def?.race?.Humanlike == true)
                {
                    AutoNudistOutfit(p);
                }
            }
        }

        [HarmonyPatch(typeof(InteractionWorker_EnslaveAttempt), nameof(InteractionWorker_EnslaveAttempt.Interacted))]
        public static class Patch_InteractionWorker_EnslaveAttempt
        {
            // Patching for enslaved prisoners
            public static void Postfix(Pawn initiator, Pawn recipient)
            {
                if (recipient is Pawn p && p.GuestStatus == GuestStatus.Slave)
                {
                    AutoNudistOutfit(p);
                }
            }
        }

        static AutomaticNudistOutfit()
        {
            Harmony harmony = new Harmony("AutomaticNudistOutfit_Ben");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        [DefOf]
        public static class TraitDefOf
        {
            public static TraitDef Nudist;
        }
        public static class OutfitLabel
        {
            public static Outfit Nudist;
        }

    }

    class WorldComp : WorldComponent
    {
        // Using a HashSet for quick lookup
        public static HashSet<Pawn> PawnsWithNudist = new HashSet<Pawn>();
        // I've found it easier to have a null list for use when exposing data
        // and HashSet will fail if more than one null value is added.
        private List<Pawn> usedForExposingData = null;

        public WorldComp(World w) : base(w)
        {
            // Make sure the static HashSet is cleared whenever a game is created or loaded.
            PawnsWithNudist.Clear();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                // When saving, populate the list
                usedForExposingData = new List<Pawn>(PawnsWithNudist);
            }

            Scribe_Collections.Look(ref usedForExposingData, "pawnsWithNudist", LookMode.Reference);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                // When loading, clear the HashSet then populate it with the loaded data
                PawnsWithNudist.Clear();
                foreach (var v in usedForExposingData)
                {
                    // Remove any null records
                    if (v != null)
                    {
                        PawnsWithNudist.Add(v);
                    }
                }
            }

            if (Scribe.mode == LoadSaveMode.Saving ||
                Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                // Add hints to the garbage collector that this memory can be collected
                usedForExposingData?.Clear();
                usedForExposingData = null;
            }
        }
    }
}