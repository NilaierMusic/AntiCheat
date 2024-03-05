﻿using BepInEx;
using BepInEx.Configuration;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using static UnityEngine.GraphicsBuffer;

namespace AntiCheat
{
    public static class Patch
    {
        public static List<ulong> jcs = new List<ulong>();

        public static List<long> zdcd = new List<long>();
        public static List<long> dgcd = new List<long>();
        public static Dictionary<ulong, List<string>> czcd = new Dictionary<ulong, List<string>>();
        public static Dictionary<ulong, List<string>> sdqcd = new Dictionary<ulong, List<string>>();

        public static Dictionary<int, Dictionary<ulong, List<DateTime>>> chcs = new Dictionary<int, Dictionary<ulong, List<DateTime>>>();

        public static List<long> mjs { get; set; } = new List<long>();
        public static int count { get; set; } = 0;
        public static List<int> AllowDespawn { get; set; }
        public static List<long> lhs { get; set; } = new List<long>();

        public static List<int> landMines { get; set; }

        public static void LogInfo(string info)
        {
            if (AntiCheatPlugin.Log.Value)
            {
                AntiCheatPlugin.ManualLog.LogInfo(info);
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), "__rpc_handler_1346025125")]
        [HarmonyPrefix]
        public static bool __rpc_handler_1346025125(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p))
            {
                ByteUnpacker.ReadValueBitPacked(reader, out int playerId);
                reader.ReadValueSafe(out bool spawnBody, default);
                reader.ReadValueSafe(out Vector3 bodyVelocity);
                ByteUnpacker.ReadValueBitPacked(reader, out int num);
                reader.Seek(0);
                if (!spawnBody)
                {
                    if (AllowDespawn == null)
                    {
                        AllowDespawn = new List<int>();
                    }
                    foreach (var item in p.ItemSlots)
                    {
                        if (item != null)
                        {
                            AllowDespawn.Add(item.GetInstanceID());
                        }
                    }
                }
                if ((CauseOfDeath)num == CauseOfDeath.Abandoned)
                {
                    bypass = true;
                    string msg = LocalizationManager.GetString("msg_behind_plaer", new Dictionary<string, string>() {
                        { "{player}",p.playerUsername }
                    });
                    LogInfo(msg);
                    HUDManager.Instance.AddTextToChatOnServer(msg, -1);
                    bypass = false;
                }
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "__rpc_handler_638895557")]
        [HarmonyPrefix]
        public static bool __rpc_handler_638895557(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p))
            {
                try
                {
                    LogInfo("__rpc_handler_638895557");
                    int damageAmount;
                    ByteUnpacker.ReadValueBitPacked(reader, out damageAmount);
                    reader.Seek(0);
                    var p2 = (PlayerControllerB)target;
                    return CheckDamage(p2, p, ref damageAmount);
                }
                catch (Exception)
                {

                }
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }

        private static bool CheckDamage(PlayerControllerB p2, PlayerControllerB p, ref int damageAmount)
        {
            if (damageAmount == 0)
            {
                return true;
            }
            LogInfo("damageAmount:" + damageAmount);
            try
            {
                if (AntiCheatPlugin.Shovel.Value)
                {
                    var distance = Vector3.Distance(p.transform.position, p2.transform.position);
                    var obj = p.ItemSlots[p.currentItemSlot];
                    string playerUsername = p.playerUsername;
                    if (damageAmount != 10 && obj != null && isShovel(obj))
                    {
                        if (!jcs.Contains(p.playerSteamId))
                        {
                            ShowMessage(LocalizationManager.GetString("msg_Shovel", new Dictionary<string, string>() {
                                { "{player}",p.playerUsername },
                                { "{player2}",p2.playerUsername },
                                { "{damageAmount}",damageAmount.ToString() }
                            }));
                            jcs.Add(p.playerSteamId);
                            if (AntiCheatPlugin.Shovel2.Value)
                            {
                                KickPlayer(p);
                            }
                        }
                        damageAmount = 0;
                    }
                    else if (distance > 11 && obj != null && isShovel(obj))
                    {
                        if (p2.isPlayerDead)
                        {
                            return true;
                        }
                        if (!jcs.Contains(p.playerSteamId))
                        {
                            ShowMessage(LocalizationManager.GetString("msg_Shovel2", new Dictionary<string, string>() {
                                { "{player}",p.playerUsername },
                                { "{player2}",p2.playerUsername },
                                { "{distance}",distance.ToString() },
                                { "{damageAmount}",damageAmount.ToString() }
                            }));
                            jcs.Add(p.playerSteamId);
                            if (AntiCheatPlugin.Shovel2.Value)
                            {
                                KickPlayer(p);
                            }
                        }
                        damageAmount = 0;
                    }
                    else if (obj == null || (!isGun(obj) && !isShovel(obj)))
                    {
                        if (damageAmount != 10)
                        {
                            if (!jcs.Contains(p.playerSteamId))
                            {
                                ShowMessage(LocalizationManager.GetString("msg_Shovel3", new Dictionary<string, string>() {
                                    { "{player}",p.playerUsername },
                                    { "{player2}",p2.playerUsername },
                                    { "{damageAmount}",damageAmount.ToString() }
                                }));
                                jcs.Add(p.playerSteamId);
                                if (AntiCheatPlugin.Shovel2.Value)
                                {
                                    KickPlayer(p);
                                }
                            }
                        }
                        damageAmount = 0;
                    }
                    else if (jcs.Contains(p.playerSteamId))
                    {
                        damageAmount = 0;
                    }
                    if (damageAmount == 0)
                    {
                        LogInfo("115");
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogInfo($"{ex.ToString()}");
            }
            return true;
        }


        //[HarmonyPatch(typeof(PlayerControllerB), "DamagePlayerFromOtherClientServerRpc")]
        //[HarmonyPrefix]
        //public static bool DamagePlayerFromOtherClientServerRpc(PlayerControllerB __instance, ref int damageAmount, Vector3 hitDirection, int playerWhoHit)
        //{
        //    return DamagePlayerFromOtherClientServerRpc(ref damageAmount, playerWhoHit);
        //}



        public static bool isGun(GrabbableObject item)
        {
            return item is ShotgunItem;
        }

        public static bool isShovel(GrabbableObject item)
        {
            return item is Shovel;
        }

        public static bool isJetpack(GrabbableObject item)
        {
            return item is JetpackItem;
        }

        public static ulong lastClientId { get; set; }


        [HarmonyPatch(typeof(StartOfRound), "StartTrackingAllPlayerVoices")]
        [HarmonyPostfix]
        public static void StartTrackingAllPlayerVoices()
        {
            if (!StartOfRound.Instance.localPlayerController.IsHost)
            {
                return;
            }
            foreach (var item in StartOfRound.Instance.allPlayerScripts)
            {
                if (!item.isPlayerControlled)
                {
                    continue;
                }
                var playerName = item.playerUsername;
                if (playerName == "Player #0")
                {
                    continue;
                }
                if (StartOfRound.Instance.KickedClientIds.Contains(item.playerSteamId))
                {
                    KickPlayer(item);
                    return;
                }
                LogInfo(playerName);
                if (Regex.IsMatch(playerName, "Nameless\\d*") || Regex.IsMatch(playerName, "Unknown\\d*") || Regex.IsMatch(playerName, "Player #\\d*"))
                {
                    if (AntiCheatPlugin.Nameless.Value)
                    {
                        ShowMessage(LocalizationManager.GetString("msg_Nameless"));
                        if (AntiCheatPlugin.Nameless2.Value)
                        {
                            KickPlayer(item, true);
                        }
                    }
                }
            }
            var p2 = StartOfRound.Instance.allPlayerScripts.OrderByDescending(x => x.actualClientId).FirstOrDefault();
            if (p2.actualClientId != lastClientId)
            {
                if (p2.isPlayerControlled && p2.playerSteamId == 0)
                {
                    KickPlayer(p2);
                    return;
                }
                else if (!p2.isPlayerControlled)
                {
                    return;
                }
                bypass = true;
                string msg = LocalizationManager.GetString("msg_wlc_player", new Dictionary<string, string>() {
                    { "{player}",p2.playerUsername }
                });
                LogInfo(msg);
                HUDManager.Instance.AddTextToChatOnServer(msg, -1);
                lastClientId = p2.actualClientId;
                bypass = false;
            }
        }

        [HarmonyPatch(typeof(HUDManager), "__rpc_handler_1944155956")]
        [HarmonyPrefix]
        public static bool __rpc_handler_1944155956(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p))
            {
                string msg = LocalizationManager.GetString("msg_snc_player", new Dictionary<string, string>() {
                    { "{player}",p.playerUsername }
                });
                LogInfo(msg);
                HUDManager.Instance.AddTextToChatOnServer(msg, -1);
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(GameNetworkManager), "SteamMatchmaking_OnLobbyMemberJoined")]
        [HarmonyPostfix]
        public static void SteamMatchmaking_OnLobbyMemberJoined()
        {
            if (!StartOfRound.Instance.localPlayerController.IsHost)
            {
                return;
            }
            LogInfo($"SetMoney:{Money}");
            Money = UnityEngine.Object.FindObjectOfType<Terminal>().groupCredits;
            jcs = new List<ulong>();
            chcs = new Dictionary<int, Dictionary<ulong, List<DateTime>>>();
        }

        public static int Money = -1;

        [HarmonyPatch(typeof(StartOfRound), "__rpc_handler_3953483456")]
        [HarmonyPrefix]
        public static bool __rpc_handler_3953483456(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p))
            {
                if (AntiCheatPlugin.FreeBuy.Value)
                {
                    int unlockableID;
                    ByteUnpacker.ReadValueBitPacked(reader, out unlockableID);
                    int newGroupCreditsAmount;
                    ByteUnpacker.ReadValueBitPacked(reader, out newGroupCreditsAmount);
                    reader.Seek(0);
                    LogInfo($"__rpc_handler_3953483456|newGroupCreditsAmount:{newGroupCreditsAmount}");
                    if (Money == newGroupCreditsAmount || Money == 0)
                    {
                        LogInfo($"Money:{Money}|newGroupCreditsAmount:{newGroupCreditsAmount}");
                        ShowMessage(LocalizationManager.GetString("msg_FreeBuy_unlockable", new Dictionary<string, string>() {
                            { "{player}",p.playerUsername }
                        }));
                        if (AntiCheatPlugin.FreeBuy2.Value)
                        {
                            KickPlayer(p);
                        }
                        return false;
                    }
                    else if (newGroupCreditsAmount > Money)
                    {
                        ShowMessage(LocalizationManager.GetString("msg_FreeBuy_GiveMoney", new Dictionary<string, string>() {
                            { "{player}",p.playerUsername }
                        }));
                        if (AntiCheatPlugin.FreeBuy2.Value)
                        {
                            KickPlayer(p);
                        }
                        return false;
                    }
                }
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }





        [HarmonyPatch(typeof(StartOfRound), "BuyShipUnlockableClientRpc")]
        [HarmonyPrefix]
        public static bool BuyShipUnlockableClientRpc(int newGroupCreditsAmount, int unlockableID = -1)
        {
            if (!StartOfRound.Instance.localPlayerController.IsHost)
            {
                return true;
            }
            Money = newGroupCreditsAmount;
            return true;
        }

        [HarmonyPatch(typeof(Terminal), "__rpc_handler_4003509079")]
        [HarmonyPrefix]
        public static bool __rpc_handler_4003509079(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p))
            {
                if (AntiCheatPlugin.FreeBuy.Value)
                {
                    reader.ReadValueSafe(out bool flag, default);
                    int[] boughtItems = null;
                    if (flag)
                    {
                        reader.ReadValueSafe(out boughtItems, default);
                    }
                    ByteUnpacker.ReadValueBitPacked(reader, out int newGroupCredits);
                    reader.Seek(0);
                    LogInfo("__rpc_handler_4003509079|boughtItems:" + string.Join(",", boughtItems) + "|newGroupCredits:" + newGroupCredits + "|Money:" + Money);
                    if (Money == newGroupCredits || Money == 0)
                    {
                        ShowMessage(LocalizationManager.GetString("msg_FreeBuy_Item", new Dictionary<string, string>() {
                            { "{player}",p.playerUsername }
                        }));
                        if (AntiCheatPlugin.FreeBuy2.Value)
                        {
                            KickPlayer(p);
                        }
                        return false;
                    }
                    else if (newGroupCredits > Money)
                    {
                        ShowMessage(LocalizationManager.GetString("msg_FreeBuy_GiveMoney", new Dictionary<string, string>() {
                            { "{player}",p.playerUsername }
                        }));
                        if (AntiCheatPlugin.FreeBuy2.Value)
                        {
                            KickPlayer(p);
                        }
                        return false;
                    }
                }
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Terminal), "SyncGroupCreditsClientRpc")]
        [HarmonyPrefix]
        public static bool SyncGroupCreditsClientRpc(int newGroupCredits, int numItemsInShip)
        {
            if (!StartOfRound.Instance.localPlayerController.IsHost)
            {
                return true;
            }
            Money = newGroupCredits;
            return true;
        }

        [HarmonyPatch(typeof(StartOfRound), "ChangeLevelClientRpc")]
        [HarmonyPrefix]
        public static bool ChangeLevelClientRpc(int levelID, int newGroupCreditsAmount)
        {
            if (!StartOfRound.Instance.localPlayerController.IsHost)
            {
                return true;
            }
            Money = newGroupCreditsAmount;
            return true;
        }


        [HarmonyPatch(typeof(Terminal), "BeginUsingTerminal")]
        [HarmonyPrefix]
        public static bool BeginUsingTerminal(Terminal __instance)
        {
            Money = __instance.groupCredits;
            LogInfo($"SetMoney:{Money}");
            return true;
        }



        [HarmonyPatch(typeof(PlayerControllerB), "__rpc_handler_1084949295")]
        [HarmonyPrefix]
        public static bool __rpc_handler_1084949295(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p))
            {
                ByteUnpacker.ReadValueBitPacked(reader, out int damageNumber);
                ByteUnpacker.ReadValueBitPacked(reader, out int newHealthAmount);
                reader.Seek(0);
                var p2 = (PlayerControllerB)target;
                if (p2 == p)
                {
                    return true;
                }
                return CheckDamage(p2, p, ref damageNumber);
            }
            return true;
        }

        /// <summary>
        /// Prefix EnemyAI.SwitchToBehaviourServerRpc
        /// </summary>
        [HarmonyPatch(typeof(EnemyAI), "__rpc_handler_2081148948")]
        [HarmonyPrefix]
        public static bool __rpc_handler_2081148948(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p))
            {
                ByteUnpacker.ReadValueBitPacked(reader, out int stateIndex);
                reader.Seek(0);
                LogInfo($"{p.playerUsername} call EnemyAI.SwitchToBehaviourServerRpc|stateIndex:{stateIndex}");
                if (AntiCheatPlugin.Enemy.Value)
                {
                    var e = (EnemyAI)target;
                    if (e is JesterAI j)
                    {
                        if (j.currentBehaviourStateIndex == 0 && stateIndex == 1)
                        {
                            if (j.targetPlayer != null && j.beginCrankingTimer <= 0f)
                            {
                                return true;
                            }
                        }
                        else if (j.currentBehaviourStateIndex == 1 && stateIndex == 2 && j.stunNormalizedTimer <= 0f && j.popUpTimer <= 0)
                        {
                            return true;
                        }
                        else if (j.currentBehaviourStateIndex == 2 && stateIndex == 0)
                        {
                            return true;
                        }
                        else
                        {
                            ShowMessage($"检测到玩家 {p.playerUsername} 强制改变怪物状态！");
                            return false;
                        }
                    }
                }
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }

        #region Enemy Kill Player(Only Player Self)

        /// <summary>
        /// Prefix JesterAI.KillPlayerServerRpc
        /// </summary>
        [HarmonyPatch(typeof(JesterAI), "__rpc_handler_3446243450")]
        [HarmonyPrefix]
        public static bool __rpc_handler_3446243450(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            return KillPlayerServerRpc(target, reader, rpcParams);
        }

        /// <summary>
        /// Prefix MouthDogAI.KillPlayerServerRpc
        /// </summary>
        [HarmonyPatch(typeof(MouthDogAI), "__rpc_handler_998670557")]
        [HarmonyPrefix]
        public static bool __rpc_handler_998670557(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            return KillPlayerServerRpc(target, reader, rpcParams);
        }

        /// <summary>
        /// Prefix ForestGiantAI.GrabPlayerServerRpc
        /// </summary>
        [HarmonyPatch(typeof(ForestGiantAI), "__rpc_handler_2965927486")]
        [HarmonyPrefix]
        public static bool __rpc_handler_2965927486(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            return KillPlayerServerRpc(target, reader, rpcParams);
        }

        /// <summary>
        /// Prefix RedLocustBees.BeeKillPlayerServerRpc
        /// </summary>
        [HarmonyPatch(typeof(RedLocustBees), "__rpc_handler_3246315153")]
        [HarmonyPrefix]
        public static bool __rpc_handler_3246315153(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            return KillPlayerServerRpc(target, reader, rpcParams);
        }

        /// <summary>
        /// Prefix MaskedPlayerEnemy.KillPlayerAnimationServerRpc
        /// </summary>
        [HarmonyPatch(typeof(MaskedPlayerEnemy), "__rpc_handler_3192502457")]
        [HarmonyPrefix]
        public static bool __rpc_handler_3192502457(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            return KillPlayerServerRpc(target, reader, rpcParams);
        }

        /// <summary>
        /// Prefix NutcrackerEnemyAI.LegKickPlayerServerRpc
        /// </summary>
        [HarmonyPatch(typeof(NutcrackerEnemyAI), "__rpc_handler_3881699224")]
        [HarmonyPrefix]
        public static bool __rpc_handler_3881699224(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            return KillPlayerServerRpc(target, reader, rpcParams);
        }

        /// <summary>
        /// Prefix BlobAI.SlimeKillPlayerEffectServerRpc
        /// </summary>
        [HarmonyPatch(typeof(BlobAI), "__rpc_handler_3848306567")]
        [HarmonyPrefix]
        public static bool __rpc_handler_3848306567(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            return KillPlayerServerRpc(target, reader, rpcParams);
        }

        /// <summary>
        /// Prefix CentipedeAI.ClingToPlayerServerRpc
        /// </summary>
        [HarmonyPatch(typeof(CentipedeAI), "__rpc_handler_2791977891")]
        [HarmonyPrefix]
        public static bool __rpc_handler_2791977891(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            return KillPlayerServerRpc(target, reader, rpcParams);
        }

        private static bool KillPlayerServerRpc(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p))
            {
                var e = (EnemyAI)target;
                ByteUnpacker.ReadValueBitPacked(reader, out int playerId);
                reader.Seek(0);
                LogInfo($"{p.playerUsername} call {e.GetType()}.KillPlayerServerRpc|playerId:{playerId}");
                if (playerId <= StartOfRound.Instance.allPlayerScripts.Length && StartOfRound.Instance.allPlayerScripts[playerId] != p)
                {
                    LogInfo($"{p.playerUsername} call {e.GetType()}.KillPlayerServerRpc|playerUsername:{StartOfRound.Instance.allPlayerScripts[playerId].playerUsername}");
                    return false;
                }
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }

        #endregion

        /// <summary>
        /// Prefix EnemyAI.UpdateEnemyPositionServerRpc
        /// </summary>
        [HarmonyPatch(typeof(EnemyAI), "__rpc_handler_255411420")]
        [HarmonyPrefix]
        public static bool __rpc_handler_255411420(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p))
            {
                reader.ReadValueSafe(out Vector3 newPos);
                reader.Seek(0);
                LogInfo($"{p.playerUsername} call EnemyAI.UpdateEnemyPositionServerRpc|newPos:{newPos}");
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }


        [HarmonyPatch(typeof(EnemyAI), "__rpc_handler_1810146992")]
        [HarmonyPrefix]
        public static bool __rpc_handler_1810146992(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p))
            {
                LogInfo($"{p.playerUsername} call EnemyAI.KillEnemyServerRpc");
                if (AntiCheatPlugin.KillEnemy.Value)
                {
                    var e = (EnemyAI)target;
                    if (e.enemyHP <= 0)
                    {
                        return true;
                    }
                    if (Vector3.Distance(p.transform.position, e.transform.position) > 50f)
                    {
                        ShowMessage(LocalizationManager.GetString("msg_Enemy_ChangeOwnershipOfEnemy", new Dictionary<string, string>() {
                            { "{player}",p.playerUsername },
                            { "{enemyName}",e.enemyType.enemyName },
                            { "{HP}",e.enemyHP.ToString() }
                        }));
                        if (AntiCheatPlugin.KillEnemy2.Value)
                        {
                            KickPlayer(p);
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(EnemyAI), "__rpc_handler_3079913705")]
        [HarmonyPrefix]
        public static bool __rpc_handler_3079913705(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p))
            {
                if (AntiCheatPlugin.Enemy.Value)
                {
                    LogInfo($"{p.playerUsername} call EnemyAI.UpdateEnemyRotationServerRpc");
                    return true;
                }
            }
            return true;
        }

        //[HarmonyPatch(typeof(EnemyAI), "__rpc_handler_255411420")]
        //[HarmonyPrefix]
        //public static bool __rpc_handler_255411420(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        //{
        //    if (Check(rpcParams, out var p))
        //    {
        //        if (AntiCheatPlugin.Enemy.Value)
        //        {
        //            LogInfo($"{p.playerUsername} call EnemyAI.UpdateEnemyPositionServerRpc");
        //            return true;
        //        }
        //    }
        //    else if (p == null)
        //    {
        //        return false;
        //    }
        //    return true;
        //}

        [HarmonyPatch(typeof(EnemyAI), "__rpc_handler_3587030867")]
        [HarmonyPrefix]
        public static bool __rpc_handler_3587030867(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            LogInfo("__rpc_handler_3587030867");
            if (Check(rpcParams, out var p))
            {
                if (AntiCheatPlugin.Enemy.Value)
                {
                    ByteUnpacker.ReadValueBitPacked(reader, out int clientId);
                    reader.Seek(0);
                    var enemy = target.GetComponent<EnemyAI>();
                    if (enemy is MouthDogAI || enemy is DressGirlAI)
                    {
                        return true;
                    }
                    float v = Vector3.Distance(p.transform.position, enemy.transform.position);
                    LogInfo($"Distance:{v}");
                    LogInfo($"{p.playerUsername}|{p.actualClientId} called ChangeOwnershipOfEnemy|enemy:{enemy.enemyType.enemyName}|clientId:{clientId}");
                    LogInfo($"OwnerClientId:{enemy.OwnerClientId}");
                    int key = enemy.GetInstanceID();
                    if (!chcs.ContainsKey(key))
                    {
                        chcs.Add(key, new Dictionary<ulong, List<DateTime>>());
                    }
                    if (!chcs[key].ContainsKey(p.actualClientId))
                    {
                        chcs[key].Add(p.actualClientId, new List<DateTime>());
                    }
                    if (chcs[key][p.actualClientId].Count(x => x.AddSeconds(1) > DateTime.Now) > 5)
                    {
                        ShowMessage(LocalizationManager.GetString("msg_Enemy_ChangeOwnershipOfEnemy", new Dictionary<string, string>() {
                            { "{player}",p.playerUsername }
                        }));
                        //KickPlayer(p); 
                        return false;
                    }
                    chcs[key][p.actualClientId].Add(DateTime.Now);
                    if (enemy.isEnemyDead)
                    {
                        return false;
                    }
                    else if (enemy is CrawlerAI c)
                    {
                        if (c.stunnedByPlayer != null)
                        {
                            if ((int)c.stunnedByPlayer.actualClientId != clientId)
                            {
                                return false;
                            }
                        }
                        else
                        {
                            var p2 = enemy.CheckLineOfSightForPlayer(55f, 60, -1);
                            if (p2 != null && (int)p2.actualClientId != clientId)
                            {
                                return false;
                            }

                        }
                    }
                    else if (enemy is JesterAI j)
                    {
                        if (j.currentBehaviourStateIndex != 2)
                        {
                            LogInfo("client ChangeOwnershipOfEnemy:" + clientId);
                            return p.isHostPlayerObject;
                        }
                        else
                        {
                            if (j.TargetClosestPlayer(4f, false, 70f) && j.targetPlayer != null && (int)j.targetPlayer.actualClientId == clientId)
                            {
                                return true;
                            }
                            return false;
                        }
                    }
                    else if (enemy is FlowermanAI f)
                    {
                        if (f.currentBehaviourStateIndex != 2)
                        {
                            LogInfo("client ChangeOwnershipOfEnemy:" + clientId);
                            return p.isHostPlayerObject;
                        }
                        else
                        {
                            if (f.TargetClosestPlayer(1.5f, false, 70f) && f.targetPlayer != null && (int)f.targetPlayer.actualClientId == clientId)
                            {
                                return true;
                            }
                            return false;
                        }
                    }
                    else if (enemy is RedLocustBees r)
                    {
                        var p2 = r.CheckLineOfSightForPlayer(360f, 16, 1);
                        if (p2 != null && Vector3.Distance(p2.transform.position, r.hive.transform.position) < (float)r.defenseDistance && (int)r.targetPlayer.actualClientId == clientId)
                        {
                            return true;
                        }
                        return false;
                    }
                    return true;
                }
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(JetpackItem), "__rpc_handler_3663112878")]
        [HarmonyPrefix]
        public static bool __rpc_handler_3663112878(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p))
            {
                if (AntiCheatPlugin.Jetpack.Value)
                {
                    var jp = (JetpackItem)target;
                    if (jp.playerHeldBy != null && jp.playerHeldBy.actualClientId != p.actualClientId)
                    {
                        ShowMessage(LocalizationManager.GetString("msg_Jetpack", new Dictionary<string, string>() {
                             { "{player}",p.playerUsername }
                        }));
                        if (AntiCheatPlugin.Jetpack2.Value)
                        {
                            KickPlayer(p);
                        }
                        return false;
                    }
                    else if (jp.playerHeldBy == null)
                    {
                        ShowMessage(LocalizationManager.GetString("msg_Jetpack", new Dictionary<string, string>() {
                             { "{player}",p.playerUsername }
                        }));
                        if (AntiCheatPlugin.Jetpack2.Value)
                        {
                            KickPlayer(p);
                        }
                        return false;
                    }
                    return true;
                }
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }


        [HarmonyPatch(typeof(EnemyAI), "__rpc_handler_2814283679")]
        [HarmonyPrefix]
        public static bool HitEnemyServerRpcPatch(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p) || StartOfRound.Instance.localPlayerController.IsHost)
            {
                if (p == null)
                {
                    return false;
                }
                LogInfo("__rpc_handler_2814283679");
                int force;
                ByteUnpacker.ReadValueBitPacked(reader, out force);
                ByteUnpacker.ReadValueBitPacked(reader, out int playerWhoHit);
                reader.ReadValueSafe(out bool playHitSFX, default);
                reader.Seek(0);
                AntiCheatPlugin.ManualLog.LogDebug("force:" + force);
                AntiCheatPlugin.ManualLog.LogDebug("playerWhoHit:" + playerWhoHit);
                if (p.isHostPlayerObject)
                {
                    return true;
                }
                AntiCheatPlugin.ManualLog.LogDebug("playerWhoHit != -1 " + (playerWhoHit != -1));
                AntiCheatPlugin.ManualLog.LogDebug("StartOfRound.Instance.allPlayerScripts[playerWhoHit] != p " + (StartOfRound.Instance.allPlayerScripts[playerWhoHit] != p));
                if (playerWhoHit != -1 && StartOfRound.Instance.allPlayerScripts[playerWhoHit] != p)
                {
                    AntiCheatPlugin.ManualLog.LogDebug("return false;");
                    return false;
                }
                if (AntiCheatPlugin.Shovel.Value)
                {
                    var obj = p.ItemSlots[p.currentItemSlot];
                    string playerUsername = p.playerUsername;
                    if (force != 1 && obj != null && isShovel(obj))
                    {
                        if (!jcs.Contains(p.playerSteamId))
                        {
                            var e = (EnemyAI)target;
                            ShowMessage(LocalizationManager.GetString("msg_Shovel4", new Dictionary<string, string>() {
                                { "{player}",p.playerUsername },
                                { "{enemyName}",e.enemyType.enemyName },
                                { "{damageAmount}",force.ToString() }
                            }));
                            jcs.Add(p.playerSteamId);
                            if (AntiCheatPlugin.Shovel2.Value)
                            {
                                KickPlayer(p);
                            }
                            return false;
                        }
                    }
                    else if (!p.isPlayerDead && obj == null && force != 1)
                    {
                        if (!jcs.Contains(p.playerSteamId))
                        {
                            var e = (EnemyAI)target;
                            ShowMessage(LocalizationManager.GetString("msg_Shovel6", new Dictionary<string, string>() {
                                { "{player}",p.playerUsername },
                                { "{enemyName}",e.enemyType.enemyName },
                                { "{damageAmount}",force.ToString() }
                            }));
                            jcs.Add(p.playerSteamId);
                            if (AntiCheatPlugin.Shovel2.Value)
                            {
                                KickPlayer(p);
                            }
                            return false;
                        }
                    }
                    else if (jcs.Contains(p.playerSteamId))
                    {
                        return false;
                    }
                }
            }

            return true;
        }




        public static bool CheckSelf(PlayerControllerB p, int playerId)
        {
            if (playerId <= StartOfRound.Instance.allPlayerScripts.Length)
            {
                var p2 = StartOfRound.Instance.allPlayerScripts[playerId];
                if (p2.playerSteamId == p.playerSteamId)
                {
                    return true;
                }
            }
            return false;
        }



        private static string lastMessage = string.Empty;

        public static void ShowMessage(string msg, string lastmsg = null)
        {
            if (lastMessage == msg || lastmsg == lastMessage)
            {
                return;
            }
            if (lastmsg != null)
            {
                lastMessage = lastmsg;
            }
            else
            {
                lastMessage = msg;
            }
            bypass = true;
            string showmsg = LocalizationManager.GetString("MessageFormat", new Dictionary<string, string>() {
                { "{Prefix}",LocalizationManager.GetString("Prefix") },
                { "{msg}",msg }
            });
            LogInfo(showmsg);
            HUDManager.Instance.AddTextToChatOnServer(showmsg, -1);
            bypass = false;
        }

        public static bool bypass { get; set; }

        [HarmonyPatch(typeof(GiftBoxItem), "__rpc_handler_2878544999")]
        [HarmonyPrefix]
        public static bool __rpc_handler_2878544999(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p))
            {
                if (AntiCheatPlugin.Gift.Value)
                {
                    if (lhs.Contains(target.GetInstanceID()))
                    {
                        ShowMessage(LocalizationManager.GetString("msg_Gift", new Dictionary<string, string>() {
                            { "{player}",p.playerUsername }
                        }));
                        if (AntiCheatPlugin.Gift2.Value)
                        {
                            KickPlayer(p);
                        }
                        return false;
                    }
                    else
                    {
                        lhs.Add(target.GetInstanceID());
                        var item = (GiftBoxItem)target;
                        item.OnNetworkDespawn();
                    }
                }
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(HauntedMaskItem), "__rpc_handler_1065539967")]
        [HarmonyPrefix]
        public static bool __rpc_handler_1065539967(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p))
            {
                if (AntiCheatPlugin.Mask.Value)
                {
                    if (mjs.Contains(target.GetInstanceID()))
                    {
                        ShowMessage(LocalizationManager.GetString("msg_Mask", new Dictionary<string, string>() {
                            { "{player}",p.playerUsername }
                        }));
                        if (AntiCheatPlugin.Mask2.Value)
                        {
                            KickPlayer(p);
                        }
                        return false;
                    }
                    else
                    {
                        mjs.Add(target.GetInstanceID());
                    }
                }
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }

        private static bool Check(__RpcParams rpcParams, out PlayerControllerB p)
        {
            if (StartOfRound.Instance.localPlayerController == null)
            {
                p = default;
                return false;
            }
            else if (!StartOfRound.Instance.localPlayerController.IsHost)//非主机
            {
                p = StartOfRound.Instance.localPlayerController;
                return false;
            }
            var tmp = GetPlayer(rpcParams);
            p = tmp;
            if (p == null)//没玩家
            {
                NetworkManager.Singleton.DisconnectClient(rpcParams.Server.Receive.SenderClientId);
                return false;
            }
            else if (rpcParams.Server.Receive.SenderClientId == GameNetworkManager.Instance.localPlayerController.actualClientId)//非本地
            {
                p = StartOfRound.Instance.localPlayerController;
                return false;
            }
            else if (StartOfRound.Instance.KickedClientIds.Contains(p.playerSteamId))//如果被踢
            {
                NetworkManager.Singleton.DisconnectClient(rpcParams.Server.Receive.SenderClientId);
                return false;
            }
            else if (p.playerSteamId == 0)
            {
                NetworkManager.Singleton.DisconnectClient(rpcParams.Server.Receive.SenderClientId);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(RoundManager), "LoadNewLevel")]
        [HarmonyPrefix]
        public static bool LoadNewLevel(SelectableLevel newLevel)
        {
            if (!StartOfRound.Instance.localPlayerController.IsHost)//非主机
            {
                return true;
            }
            landMines = new List<int>();
            AllowDespawn = new List<int>();
            HUDManager.Instance.AddTextToChatOnServer(LocalizationManager.GetString("msg_game_start", new Dictionary<string, string>() {
                { "{ver}",AntiCheatPlugin.Version }
            }), -1);
            return true;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "__rpc_handler_1554282707")]
        [HarmonyPrefix]
        public static bool __rpc_handler_1554282707(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p))
            {
                if (AntiCheatPlugin.GrabObject.Value)
                {
                    reader.ReadValueSafe(out NetworkObjectReference grabbedObject, default);
                    reader.Seek(0);
                    if (grabbedObject.TryGet(out var networkObject, null))
                    {
                        var all = true;
                        foreach (var item in p.ItemSlots)
                        {
                            if (item == null)
                            {
                                all = false;
                            }
                            else if (item.itemProperties.twoHanded)
                            {
                                all = true;
                                break;
                            }
                        }
                        var g = networkObject.GetComponentInChildren<GrabbableObject>();
                        if (g != null)
                        {
                            if (all)
                            {
                                var __rpc_exec_stage = typeof(NetworkBehaviour).GetField("__rpc_exec_stage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                __rpc_exec_stage.SetValue(target, 1);
                                typeof(PlayerControllerB).GetMethod("GrabObjectServerRpc", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke((PlayerControllerB)target, new object[] {
                                    default
                                });
                                __rpc_exec_stage.SetValue(target, 0);
                                return false;
                            }
                            else if (Vector3.Distance(g.transform.position, p.serverPlayerPosition) > 100 && !StartOfRound.Instance.shipIsLeaving && StartOfRound.Instance.shipHasLanded)
                            {
                                if (p.teleportedLastFrame)
                                {
                                    return true;
                                }
                                ShowMessage(LocalizationManager.GetString("msg_GrabObject", new Dictionary<string, string>() {
                                    { "{player}",p.playerUsername },
                                    { "{object_postion}",g.transform.position.ToString() },
                                    { "{player_postion}",p.serverPlayerPosition.ToString() }
                                }));
                                g = default;
                                grabbedObject = default;
                                if (AntiCheatPlugin.GrabObject2.Value)
                                {
                                    KickPlayer(p);
                                }
                                var __rpc_exec_stage = typeof(NetworkBehaviour).GetField("__rpc_exec_stage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                __rpc_exec_stage.SetValue(target, 1);
                                typeof(PlayerControllerB).GetMethod("GrabObjectServerRpc", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke((PlayerControllerB)target, new object[] {
                                    default
                                });
                                __rpc_exec_stage.SetValue(target, 0);
                                return false;
                            }
                        }
                    }
                }
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// PlayerControllerB.UpdatePlayerPositionServerRpc
        /// </summary>
        [HarmonyPatch(typeof(PlayerControllerB), "__rpc_handler_2013428264")]
        [HarmonyPrefix]
        public static bool __rpc_handler_2013428264(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            reader.ReadValueSafe(out Vector3 newPos);
            reader.Seek(0);
            if (Check(rpcParams, out var p))
            {
                if (AntiCheatPlugin.Invisibility.Value)
                {
                    var oldpos = p.serverPlayerPosition;
                    if (p.teleportedLastFrame)
                    {
                        return true;
                    }
                    if (Vector3.Distance(oldpos, newPos) > 100 && Vector3.Distance(newPos, new Vector3(0, 0, 0)) > 10)
                    {
                        ShowMessage(LocalizationManager.GetString("msg_Invisibility", new Dictionary<string, string>() {
                            { "{player}",p.playerUsername },
                            { "{player_postion}",newPos.ToString() }
                        }));
                        if (AntiCheatPlugin.Invisibility2.Value)
                        {
                            KickPlayer(p);
                        }
                        return false;
                    }
                }
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }



        [HarmonyPatch(typeof(PlayerControllerB), "__rpc_handler_2504133785")]
        [HarmonyPrefix]
        public static bool __rpc_handler_2504133785(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (!StartOfRound.Instance.localPlayerController.IsHost)//非主机
            {
                return true;
            }
            ByteUnpacker.ReadValueBitPacked(reader, out ulong newPlayerSteamId);
            reader.Seek(0);
            if (StartOfRound.Instance.KickedClientIds.Contains(newPlayerSteamId))
            {
                LogInfo(LocalizationManager.GetString("log_refuse_connect", new Dictionary<string, string>() {
                    { "{steamId}",newPlayerSteamId.ToString() }
                }));
                return false;
            }
            return true;
        }


        /// <summary>
        /// Prefix HUDManager.AddTextMessageServerRpc
        /// </summary>
        [HarmonyPatch(typeof(HUDManager), "__rpc_handler_2787681914")]
        [HarmonyPrefix]
        public static bool __rpc_handler_2787681914(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p))
            {
                reader.ReadValueSafe(out bool flag, default);
                string chatMessage = null;
                if (flag)
                {
                    reader.ReadValueSafe(out chatMessage, false);
                }
                reader.Seek(0);
                LogInfo($"__rpc_handler_2787681914|{p.playerUsername}{chatMessage}");
                if (chatMessage.Contains("<color") || chatMessage.Contains("<size"))
                {
                    LogInfo("playerId = -1");
                    if (AntiCheatPlugin.Map.Value)
                    {
                        if (chatMessage.Contains("<size=0>Tyzeron.Minimap"))
                        {
                            ShowMessage(LocalizationManager.GetString("msg_Map", new Dictionary<string, string>() {
                                { "{player}",p.playerUsername }
                            }));
                            if (AntiCheatPlugin.Map2.Value)
                            {
                                KickPlayer(p);
                            }
                            return false;
                        }
                    }
                    if (bypass)
                    {
                        LogInfo("bypass");
                        return true;
                    }
                    return false;
                }
                else
                {
                    return false;
                }
                return true;
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(HUDManager), "__rpc_handler_168728662")]
        [HarmonyPrefix]
        public static bool __rpc_handler_168728662(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            return __rpc_handler_2930587515(target, reader, rpcParams);
        }

        [HarmonyPatch(typeof(NetworkBehaviour), "OnNetworkDespawn")]
        [HarmonyPrefix]
        public static bool NetworkObjectDespawn(NetworkBehaviour __instance)
        {
            if (StartOfRound.Instance == null || !StartOfRound.Instance.IsHost)
            {
                return true;
            }
            PlayerControllerB p = null;
            foreach (var item in StartOfRound.Instance.allPlayerScripts)
            {
                if (item.actualClientId == __instance.OwnerClientId)
                {
                    p = item;
                }
            }
            if (p == null)
            {
                return false;
            }
            if (__instance.TryGetComponent<GrabbableObject>(out var obj))
            {
                if (AntiCheatPlugin.DespawnItem.Value)
                {
                    if (obj is GiftBoxItem || obj is KeyItem)
                    {
                        return true;
                    }
                    if (AllowDespawn.Contains(obj.GetInstanceID()))
                    {
                        return true;
                    }
                    ShowMessage(LocalizationManager.GetString("msg_DespawnItem", new Dictionary<string, string>() {
                    { "{player}",p.playerUsername },
                    { "{item}",p.currentlyHeldObjectServer.itemProperties.itemName }
                }));
                    if (AntiCheatPlugin.DespawnItem2.Value)
                    {
                        KickPlayer(p);
                    }
                    return false;
                }
            }
            return true;
        }



        [HarmonyPatch(typeof(HUDManager), "__rpc_handler_2930587515")]
        [HarmonyPrefix]
        public static bool __rpc_handler_2930587515(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p) || StartOfRound.Instance.localPlayerController.IsHost)
            {
                if (AntiCheatPlugin.ChatReal.Value)
                {
                    try
                    {
                        reader.ReadValueSafe(out bool flag, default);
                        string chatMessage = null;
                        if (flag)
                        {
                            reader.ReadValueSafe(out chatMessage, false);
                        }
                        ByteUnpacker.ReadValueBitPacked(reader, out int playerId);
                        reader.Seek(0);
                        LogInfo($"__rpc_handler_2930587515|{p.playerUsername}|{chatMessage}");
                        if (playerId == -1)
                        {
                            LogInfo("playerId = -1");
                            if (chatMessage.StartsWith("<color=red>[反作弊]") && bypass)
                            {
                                LogInfo("bypass");
                                return true;
                            }
                            if (p == StartOfRound.Instance.localPlayerController)
                            {
                                return true;
                            }
                            return false;
                        }
                        if (p == StartOfRound.Instance.localPlayerController)
                        {
                            return true;
                        }
                        if (playerId <= StartOfRound.Instance.allPlayerScripts.Length)
                        {
                            if (StartOfRound.Instance.allPlayerScripts[(int)playerId].playerSteamId != p.playerSteamId)
                            {
                                ShowMessage(LocalizationManager.GetString("msg_ChatReal", new Dictionary<string, string>() {
                                    { "{player}",p.playerUsername },
                                    { "{player2}",StartOfRound.Instance.allPlayerScripts[playerId].playerUsername },
                                }));
                                if (AntiCheatPlugin.ChatReal2.Value)
                                {
                                    KickPlayer(p);
                                }
                                return false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogInfo(ex.ToString());
                        return false;
                    }
                }
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Terminal), "__rpc_handler_1713627637")]
        [HarmonyPrefix]
        public static bool __rpc_handler_1713627637(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p))
            {
                if (AntiCheatPlugin.ShipTerminal.Value)
                {
                    DateTime dt = DateTime.Now;
                    var m = dt.Ticks / 10000;
                    if (zdcd.Count > 200)
                    {
                        zdcd.RemoveRange(0, zdcd.Count - 1);
                    }
                    if (zdcd.Contains(m))
                    {
                        return false;
                    }
                    else if (zdcd.Any(x => x + 200 > m))
                    {
                        zdcd.Add(m);
                        ShowMessage(LocalizationManager.GetString("msg_ShipTerminal", new Dictionary<string, string>() {
                            { "{player}",p.playerUsername }
                        }));
                        if (AntiCheatPlugin.ShipTerminal2.Value)
                        {
                            KickPlayer(p);
                        }
                        return false;
                    }
                    else
                    {
                        zdcd.Add(m);
                    }
                }
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(ShipLights), "__rpc_handler_1625678258")]
        [HarmonyPrefix]
        public static bool __rpc_handler_1625678258(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p))
            {
                if (AntiCheatPlugin.ShipLight.Value)
                {
                    DateTime dt = DateTime.Now;
                    var m = dt.Ticks / 10000;
                    if (dgcd.Count > 200)
                    {
                        dgcd.RemoveRange(0, dgcd.Count - 1);
                    }
                    if (dgcd.Contains(m))
                    {
                        ShipLights shipLights = UnityEngine.Object.FindAnyObjectByType<ShipLights>();
                        if (!shipLights.areLightsOn)
                        {
                            shipLights.SetShipLightsServerRpc(true);
                        }
                        return false;
                    }
                    else if (dgcd.Any(x => x + 1000 > m))
                    {
                        dgcd.Add(m);
                        ShowMessage(LocalizationManager.GetString("msg_ShipLight", new Dictionary<string, string>() {
                            { "{player}",p.playerUsername }
                        }), "msg_ShipLight");
                        ShipLights shipLights = UnityEngine.Object.FindAnyObjectByType<ShipLights>();
                        if (!shipLights.areLightsOn)
                        {
                            shipLights.SetShipLightsServerRpc(true);
                        }
                        return false;
                    }
                    else
                    {
                        dgcd.Add(m);
                    }
                }
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }

        private static PlayerControllerB GetPlayer(__RpcParams rpcParams)
        {
            foreach (var item in StartOfRound.Instance.allPlayerScripts)
            {
                if (item.actualClientId == rpcParams.Server.Receive.SenderClientId)
                {
                    return item;
                }
            }
            return null;//??
        }

        /// <summary>
        /// Prefix ShotgunItem.ShootGunServerRpc
        /// </summary>
        [HarmonyPatch(typeof(ShotgunItem), "__rpc_handler_1329927282")]
        [HarmonyPrefix]
        public static bool __rpc_handler_1329927282(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p) || StartOfRound.Instance.localPlayerController.IsHost)
            {
                if (AntiCheatPlugin.InfiniteAmmo.Value)
                {
                    var s = (ShotgunItem)target;
                    var localClientSendingShootGunRPC = (bool)typeof(ShotgunItem).GetField("localClientSendingShootGunRPC", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(s);
                    if (localClientSendingShootGunRPC)
                    {
                        return true;
                    }
                    else
                    {
                        if (s.shellsLoaded == 0)
                        {
                            ShowMessage(LocalizationManager.GetString("msg_InfiniteAmmo", new Dictionary<string, string>() {
                                { "{player}",p.playerUsername }
                            }));
                            if (AntiCheatPlugin.InfiniteAmmo2.Value)
                            {
                                KickPlayer(p);
                            }
                            return false;
                        }
                        LogInfo($"__rpc_handler_1329927282|{s.shellsLoaded}");
                    }
                }
                else if (AntiCheatPlugin.ItemCooldown.Value)
                {
                    var id = p.playerSteamId;
                    var m = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    if (!sdqcd.ContainsKey(id))
                    {
                        sdqcd.Add(id, new List<string>());
                    }
                    if (sdqcd[id].Count > 200)
                    {
                        sdqcd[id].RemoveRange(0, sdqcd[id].Count - 1);
                    }
                    if (sdqcd[id].Count(x => x == m) >= 2)
                    {
                        ShowMessage(LocalizationManager.GetString("msg_ItemCooldown", new Dictionary<string, string>() {
                            { "{player}",p.playerUsername },
                            { "{item}",LocalizationManager.GetString("Item_Shotgun") }
                        }));
                        if (AntiCheatPlugin.ItemCooldown2.Value)
                        {
                            KickPlayer(p);
                        }
                        return false;
                    }
                    else
                    {
                        sdqcd[id].Add(m);
                    }
                }
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Prefix Shovel.HitShovelServerRpc
        /// </summary>
        [HarmonyPatch(typeof(Shovel), "__rpc_handler_2096026133")]
        [HarmonyPrefix]
        public static bool __rpc_handler_2096026133(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p) || StartOfRound.Instance.localPlayerController.IsHost)
            {
                if (AntiCheatPlugin.ItemCooldown.Value)
                {
                    var id = p.playerSteamId;
                    var m = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    if (!czcd.ContainsKey(id))
                    {
                        czcd.Add(id, new List<string>());
                    }
                    if (czcd[id].Count > 200)
                    {
                        czcd[id].RemoveRange(0, czcd[id].Count - 1);
                    }
                    if (czcd[id].Count(x => x == m) >= 3)
                    {
                        ShowMessage(LocalizationManager.GetString("msg_ItemCooldown", new Dictionary<string, string>() {
                            { "{player}",p.playerUsername },
                            { "{item}",LocalizationManager.GetString("Item_Shovel") }
                        }));
                        if (AntiCheatPlugin.ItemCooldown2.Value)
                        {
                            KickPlayer(p);
                        }
                        return false;
                    }
                    else
                    {
                        czcd[id].Add(m);
                    }
                }
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(StartOfRound), "__rpc_handler_1089447320")]
        [HarmonyPrefix]
        public static bool StartGameServerRpc(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p))
            {
                if (AntiCheatPlugin.ShipConfig.Value && !GameNetworkManager.Instance.gameHasStarted)
                {
                    ShowMessage(LocalizationManager.GetString("msg_ShipConfig5", new Dictionary<string, string>() {
                        { "{player}",p.playerUsername }
                    }));
                    UnityEngine.Object.FindObjectOfType<StartMatchLever>().CancelStartGameClientRpc();
                    if (AntiCheatPlugin.ShipConfig5.Value)
                    {
                        KickPlayer(p);
                        return false;
                    }
                    return false;
                }
                else if (StartOfRound.Instance.allPlayerScripts.Count(x => x.isPlayerControlled) >= AntiCheatPlugin.ShipConfig2.Value)
                {
                    return true;
                }
                else
                {
                    UnityEngine.Object.FindObjectOfType<StartMatchLever>().CancelStartGameClientRpc();
                    ShowMessage(LocalizationManager.GetString("msg_ShipConfig2", new Dictionary<string, string>() {
                        { "{player}",p.playerUsername },
                        { "{cfg}",AntiCheatPlugin.ShipConfig2.Value.ToString() }
                    }));
                    return false;
                }
            }
            else if (p == null)
            {
                UnityEngine.Object.FindObjectOfType<StartMatchLever>().CancelStartGameClientRpc();
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(StartOfRound), "OnPlayerDC")]
        [HarmonyPostfix]
        public static void OnPlayerDC(int playerObjectNumber, ulong clientId)
        {
            PlayerControllerB component = StartOfRound.Instance.allPlayerObjects[playerObjectNumber].GetComponent<PlayerControllerB>();
            component.playerSteamId = 0;
            return;
        }


        public static PlayerControllerB whoUseTerminal { get; set; }

        [HarmonyPatch(typeof(Terminal), "__rpc_handler_4047492032")]
        [HarmonyPrefix]
        public static bool __rpc_handler_4047492032(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p))
            {
                whoUseTerminal = p;
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }

        public static void KickPlayer(PlayerControllerB kick, bool canJoin = false)
        {
            if (kick.actualClientId == 0)
            {
                return;
            }
            NetworkManager.Singleton.DisconnectClient(kick.actualClientId);
            if (kick.playerSteamId == 0)
            {
                return;
            }
            var s = StartOfRound.Instance;
            ulong playerSteamId = kick.playerSteamId;
            if (!s.KickedClientIds.Contains(playerSteamId) && !canJoin)
            {
                s.KickedClientIds.Add(playerSteamId);
            }
            var terminal = UnityEngine.Object.FindAnyObjectByType<Terminal>();
            LogInfo("terminalInUse" + terminal.placeableObject.inUse);
            LogInfo("whoUseTerminal:" + whoUseTerminal?.playerUsername);
            LogInfo("playerUsername:" + kick?.playerUsername);
            if (whoUseTerminal == kick && terminal.placeableObject.inUse)
            {
                LogInfo("SetTerminalInUseServerRpc");
                terminal.SetTerminalInUseServerRpc(false);
                terminal.terminalInUse = false;
            }
            ShowMessage(LocalizationManager.GetString("msg_Kick", new Dictionary<string, string>() {
                { "{player}",kick.playerUsername }
            }));
        }

        [HarmonyPatch(typeof(HUDManager), "SetPlayerLevel")]
        [HarmonyPrefix]
        public static bool SetPlayerLevel(bool isDead, bool mostProfitable, bool allPlayersDead)
        {
            foreach (var item in HUDManager.Instance.playerLevels)
            {
                if (item.XPMax == 0)
                {
                    item.XPMax = 1;
                }
            }
            return true;
        }

        /// <summary>
        /// Prefix StartMatchLever.PlayLeverPullEffectsServerRpc
        /// </summary>
        [HarmonyPatch(typeof(StartMatchLever), "__rpc_handler_2406447821")]
        [HarmonyPrefix]
        public static bool __rpc_handler_2406447821(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p))
            {
                if (UnityEngine.Object.FindAnyObjectByType<StartMatchLever>().leverHasBeenPulled && !StartOfRound.Instance.shipHasLanded)
                {
                    return StartGameServerRpc(target, reader, rpcParams);
                }
                else
                {
                    return EndGameServerRpc(target, reader, rpcParams);
                }
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }


        [HarmonyPatch(typeof(DepositItemsDesk), "SellAndDisplayItemProfits")]
        [HarmonyPostfix]
        public static void SellAndDisplayItemProfits(int profit, int newGroupCredits)
        {
            if (!StartOfRound.Instance.localPlayerController.IsHost)
            {
                return;
            }
            Money = newGroupCredits;
            LogInfo($"SetMoney:{Money}");
        }

        [HarmonyPatch(typeof(DepositItemsDesk), "__rpc_handler_1114072420")]
        [HarmonyPrefix]
        public static bool SellAndDisplayItemProfits(DepositItemsDesk __instance)
        {
            if (!StartOfRound.Instance.localPlayerController.IsHost)
            {
                return true;
            }
            for (int i = 0; i < __instance.itemsOnCounterNetworkObjects.Count; i++)
            {
                if (__instance.itemsOnCounterNetworkObjects[i].IsSpawned)
                {
                    AllowDespawn.Add(__instance.itemsOnCounterNetworkObjects[i].GetInstanceID());
                }
            }
            return true;
        }

        /// <summary>
        /// Prefix Turret.EnterBerserkModeServerRpc
        /// </summary>
        [HarmonyPatch(typeof(Turret), "__rpc_handler_4195711963")]
        [HarmonyPrefix]
        public static bool __rpc_handler_4195711963(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p))
            {
                if (AntiCheatPlugin.Turret.Value)
                {
                    var obj = p.ItemSlots[p.currentItemSlot];
                    if (obj != null && isShovel(obj))
                    {
                        var t = (Turret)target;
                        float v = Vector3.Distance(t.transform.position, p.transform.position);
                        if (v > 12)
                        {
                            ShowMessage(LocalizationManager.GetString("msg_Turret", new Dictionary<string, string>() {
                                { "{player}",p.playerUsername },
                                { "{Distance}",v.ToString() }
                            }));
                            if (AntiCheatPlugin.Turret2.Value)
                            {
                                KickPlayer(p);
                            }
                            return false;
                        }
                    }
                    else
                    {
                        ShowMessage(LocalizationManager.GetString("msg_Turret2", new Dictionary<string, string>() {
                             { "{player}",p.playerUsername }
                        }));
                        if (AntiCheatPlugin.Turret2.Value)
                        {
                            KickPlayer(p);
                        }
                        return false;
                    }
                }
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(SoundManager), "Update")]
        [HarmonyPrefix]
        public static void SoundManagerUpdate()
        {
            if (GameNetworkManager.Instance.localPlayerController == null || NetworkManager.Singleton == null)
            {
                count++;
                if (count >= 30)
                {
                    GameNetworkManager.Instance.LeaveCurrentSteamLobby();
                    count = 0;
                }
                return;
            }
            count = 0;
        }


        [HarmonyPatch(typeof(HUDManager), "Update")]
        [HarmonyPostfix]
        public static void Update()
        {
            if (!StartOfRound.Instance.IsHost)
            {
                return;
            }
            if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
            {
                return;
            }
            if (StartOfRound.Instance.shipIsLeaving || !StartOfRound.Instance.currentLevel.planetHasTime)
            {
                return;
            }
            if (!TimeOfDay.Instance.shipLeavingAlertCalled)
            {
                if (GameNetworkManager.Instance.localPlayerController.isPlayerDead && !string.IsNullOrEmpty(HUDManager.Instance.holdButtonToEndGameEarlyVotesText.text))
                {
                    HUDManager.Instance.holdButtonToEndGameEarlyVotesText.text += Environment.NewLine + LocalizationManager.GetString("msg_vote");
                }
            }
        }

        /// <summary>
        /// Prefix TimeOfDay.SetShipLeaveEarlyServerRpc
        /// </summary>
        [HarmonyPatch(typeof(TimeOfDay), "__rpc_handler_543987598")]
        [HarmonyPrefix]
        public static bool __rpc_handler_543987598(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p) || StartOfRound.Instance.localPlayerController.IsHost)
            {
                int num = StartOfRound.Instance.connectedPlayersAmount + 1 - StartOfRound.Instance.livingPlayers;
                if (p.isHostPlayerObject)
                {
                    if (TimeOfDay.Instance.votesForShipToLeaveEarly + 1 < num)
                    {
                        TimeOfDay.Instance.SetShipLeaveEarlyServerRpc();
                    }
                    return true;
                }
                typeof(HUDManager).GetMethod("AddChatMessage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(HUDManager.Instance, new object[] { $"{p.playerUsername} 投票让飞船提前离开({(TimeOfDay.Instance.votesForShipToLeaveEarly + 1)}/{num})", "" });
                if (TimeOfDay.Instance.votesForShipToLeaveEarly + 1 >= num)
                {
                    LogInfo("Vote EndGame");
                    return EndGameServerRpc(target, reader, rpcParams);
                }
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Prefix StartOfRound.EndGameServerRpc
        /// </summary>
        [HarmonyPatch(typeof(StartOfRound), "__rpc_handler_2028434619")]
        [HarmonyPrefix]
        public static bool EndGameServerRpc(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p))
            {
                var hour = int.Parse(AntiCheatPlugin.ShipConfig3.Value.Split(':')[0]);
                var min = int.Parse(AntiCheatPlugin.ShipConfig3.Value.Split(':')[1]);
                var time = (int)(TimeOfDay.Instance.normalizedTimeOfDay * (60f * TimeOfDay.Instance.numberOfHours)) + 360;
                int time2 = (int)Mathf.Floor((float)(time / 60));
                bool pm = false;
                if (time2 > 12)
                {
                    pm = true;
                    time2 %= 12;
                }
                time = time % 60;
                if (pm)
                {
                    time2 += 12;
                }
                var live = StartOfRound.Instance.allPlayerScripts.Where(x => x.isPlayerControlled && !x.isPlayerDead);

                if (live.Count() == 1 && live.FirstOrDefault().isInHangarShipRoom)
                {
                    return true;
                }
                decimal p1 = Math.Round((decimal)live.Count() * (decimal)(AntiCheatPlugin.ShipConfig4.Value / 100m), 2);
                decimal p2 = StartOfRound.Instance.allPlayerScripts.Count(x => x.isPlayerControlled && x.isInHangarShipRoom); // 
                if (StartOfRound.Instance.currentLevel.PlanetName.Contains("Gordion"))
                {
                    time2 = hour;
                    time = min;
                }
                if (hour <= time2 && min <= time && p2 >= p1)
                {
                    return true;
                }
                else
                {
                    ShowMessage(LocalizationManager.GetString("msg_ShipConfig4", new Dictionary<string, string>() {
                        { "{player}",p.playerUsername },
                        { "{player_count}",p1.ToString() },
                        { "{cfg4}",AntiCheatPlugin.ShipConfig4.Value.ToString() },
                        { "{cfg3}",$"{hour.ToString("00")}:{min.ToString("00")}" },
                        { "{game_time}",$"{time2.ToString("00")}:{time.ToString("00")}" }
                    }));
                    return false;
                }
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Prefix ShipBuildModeManager.PlaceShipObjectServerRpc
        /// </summary>
        /// <param name="target"></param>
        /// <param name="reader"></param>
        /// <param name="rpcParams"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(ShipBuildModeManager), "__rpc_handler_861494715")]
        [HarmonyPrefix]
        public static bool __rpc_handler_861494715(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p))
            {
                try
                {
                    if (AntiCheatPlugin.ShipBuild.Value)
                    {
                        NetworkManager networkManager = target.NetworkManager;
                        if (networkManager == null || !networkManager.IsListening)
                        {
                            return true;
                        }
                        Vector3 newPosition;
                        reader.ReadValueSafe(out newPosition);
                        Vector3 newRotation;
                        reader.ReadValueSafe(out newRotation);
                        reader.Seek(0);
                        var pl = GetPlayer(rpcParams);
                        LogInfo($"newPosition:{newPosition.ToString()}|newRotation:{newRotation.ToString()}|playerWhoMoved:{pl.playerUsername}");
                        if (newRotation.x == 0 || newPosition.x > 11.2 || newPosition.x < -5 || newPosition.y > 5 || newPosition.y < 0 || newPosition.z > -10 || newPosition.z < -18)
                        {
                            ShowMessage(LocalizationManager.GetString("msg_ShipBuild", new Dictionary<string, string>() {
                                { "{player}",p.playerUsername },
                                { "{postion}",newPosition.ToString() }
                            }));
                            if (AntiCheatPlugin.ShipBuild2.Value)
                            {
                                KickPlayer(pl);
                            }
                            return false;
                        }
                    }
                    return true;
                }
                catch (Exception)
                {

                }
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Landmine), "OnTriggerExit")]
        [HarmonyPrefix]
        public static bool OnTriggerExit(Landmine __instance, Collider other)
        {
            if (!StartOfRound.Instance.IsHost)
            {
                return true;
            }
            if (__instance.hasExploded)
            {
                return true;
            }
            bool mineActivated = (bool)typeof(Landmine).GetField("mineActivated", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(__instance);
            if (!mineActivated)
            {
                return true;
            }
            int id = __instance.GetInstanceID();
            if (!landMines.Contains(id))
            {
                landMines.Add(id);
            }
            LogInfo($"Landmine.OnTriggerExit({id})");
            return true;
        }

        [HarmonyPatch(typeof(Landmine), "__rpc_handler_3032666565")]
        [HarmonyPrefix]
        public static bool __rpc_handler_3032666565(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p))
            {
                if (AntiCheatPlugin.Landmine.Value)
                {
                    var lm = (Landmine)target;
                    int id = lm.GetInstanceID();
                    if (!landMines.Contains(id))
                    {
                        ShowMessage(LocalizationManager.GetString("msg_Landmine", new Dictionary<string, string>() {
                             { "{player}",p.playerUsername }
                        }));
                        if (AntiCheatPlugin.Landmine2.Value)
                        {
                            KickPlayer(p);
                        }
                        return false;
                    }
                }
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(DepositItemsDesk), "__rpc_handler_3230280218")]
        [HarmonyPrefix]
        public static bool __rpc_handler_3230280218(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var p))
            {
                if (AntiCheatPlugin.Boss.Value)
                {
                    ShowMessage(LocalizationManager.GetString("msg_Boss", new Dictionary<string, string>() {
                         { "{player}",p.playerUsername }
                    }));
                    if (AntiCheatPlugin.Boss2.Value)
                    {
                        KickPlayer(p);
                    }
                }
                return false;
            }
            else if (p == null)
            {
                return false;
            }
            return true;
        }


        [HarmonyPatch(typeof(GameNetworkManager), "StartHost")]
        [HarmonyPrefix]
        public static void StartHost()
        {
            if (AntiCheatPlugin.Prefix.Value.IsNullOrWhiteSpace())
            {
                return;
            }
            var setting = GameNetworkManager.Instance.lobbyHostSettings;
            string rawText = setting.lobbyName;
            rawText = rawText.Replace("【", "[").Replace("】", "]");
            List<string> labels = new List<string>();
            var match = Regex.Match(rawText, "^\\[(.*?)\\]");
            if (match.Success)
            {
                var txt = match.Groups[1].Value;
                labels.AddRange(txt.Split('/'));
                rawText = rawText.Remove(0, match.Groups[0].Value.Length).TrimStart();
            }
            if (!labels.Any(x => x == AntiCheatPlugin.Prefix.Value))
            {
                labels.Add(AntiCheatPlugin.Prefix.Value);
            }
            setting.lobbyName = "[" + string.Join("/", labels) + "]" + " " + rawText;
            if (string.IsNullOrEmpty(setting.serverTag))
            {
                setting.serverTag = AntiCheatPlugin.Prefix.Value;
            }
        }
    }
}
