using HarmonyLib;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
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

        public override void StartServerSide(ICoreServerAPI api)
        {
            // Room status command
            api.ChatCommands.Get("debug").AllSubcommands["rooms"].BeginSubCommand("status")
            .RequiresPlayer()
            .HandleWith((args) =>
            {
                // Get the room the player is in
                var player = args.Caller.Player;
                var roomRegistry = api.ModLoader.GetModSystem<RoomRegistry>();
                var playerPos = player.Entity.Pos.AsBlockPos;
                var roomAtPlayer = roomRegistry.GetRoomForPosition(playerPos);

                if (roomAtPlayer == null)
                {
                    return TextCommandResult.Error("No room found at player position");
                }
                else
                {
                    // Get room bounding box
                    string boundingBox = string.Format("{0}x{1}x{2}",
                        roomAtPlayer.Location.SizeX + 1,
                        roomAtPlayer.Location.SizeY + 1,
                        roomAtPlayer.Location.SizeZ + 1
                    );

                    // Get room cellar score and greenhouse status
                    float coolingScore = float.Clamp((float)roomAtPlayer.NonCoolingWallCount / int.Max(1, roomAtPlayer.CoolingWallCount), 0f, 1f);
                    float lightingScore = roomAtPlayer.SkylightCount / float.Max(1f, roomAtPlayer.SkylightCount + roomAtPlayer.NonSkylightCount);
                    float cellarScore = 1f - 0.5f * coolingScore - 0.4f * lightingScore;
                    string isGreenhouse = roomAtPlayer.SkylightCount > roomAtPlayer.NonSkylightCount && roomAtPlayer.ExitCount == 0 ? "Yes" : "No";

                    var response = string.Format(
                        @"Status of room at player position:
  Bounding box: {0}
  Is greenhouse: {1}
  Cellar rating: {2:P1}",
                        boundingBox,
                        isGreenhouse,
                        cellarScore
                    );

                    return TextCommandResult.Success(response);
                }
            });
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll(Mod.Info.ModID);
        }

        // Patch RoomRegistry to mark all rooms as "small"
        // In vanilla, the only thing this affects is whether the room is a cellar
        [HarmonyPostfix]
        [HarmonyPatch(typeof(RoomRegistry), "GetRoomForPosition")]
        public static void GetRoomForPositionPostfix(ref Room __result, BlockPos pos)
        {
            __result.IsSmallRoom = true;
        }
    }
}
