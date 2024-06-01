using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BiggerCellars
{
    [HarmonyPatch]
    public class BiggerCellarsModSystem : ModSystem
    {
        private static ICoreAPI api;
        private Harmony harmony;

        public override void Start(ICoreAPI api)
        {
            BiggerCellarsModSystem.api = api;

            if (!Harmony.HasAnyPatches(Mod.Info.ModID))
            {
                harmony = new Harmony(Mod.Info.ModID);
                harmony.PatchAll();
            }

            api.Logger.Notification("{0} Setup Complete", Mod.Info.ModID);
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll(Mod.Info.ModID);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(RoomRegistry), "GetRoomForPosition")]
        public static void GetRoomForPositionPostfix(ref Room __result, BlockPos pos)
        {
            __result.IsSmallRoom = true;
        }
    }
}
