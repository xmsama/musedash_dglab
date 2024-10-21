using MelonLoader;
using MuseDash_DgLab;
using HarmonyLib;
using UnityEngine;

using System.Text.Json;
using Il2CppAssets.Scripts.Common;
using Rect = UnityEngine.Rect;
using static UnityEngine.GUI;
using Type = System.Type;
using System.Text.Encodings.Web;
using static UnityEngine.RectTransform;
using Il2CppSteamworks;

[assembly: MelonInfo(typeof(ModMain), "Muse_DGLAB", "1.0", "xm")]
[assembly: MelonGame("PeroPeroGames", "MuseDash")]
namespace MuseDash_DgLab
{

    public class ModConfigData
    {
        public string Host { get; set; } = "ws://127.0.0.1:60536/1";
        public NoteRateConfig NoteRate { get; set; } = new NoteRateConfig();
        public MissConfig Miss { get; set; } = new MissConfig();
    }

    public class NoteRateConfig
    {
        public bool Enable { get; set; } = true;
        public bool EnablePass { get; set; } = false;
        public string PassWave { get; set; } = "经典";
        public int PassWaveLevel { get; set; } = 100;
        public int PassWaveTime { get; set; } = 10;
        public bool EnableGreat { get; set; } = true;
        public string GreateWave { get; set; } = "经典";
        public int GreateWaveLevel { get; set; } = 100;
        public int GreateWaveTime { get; set; } = 10;
        public bool EnablePerfect { get; set; } = false;
        public string PerfectWave { get; set; } = "经典";
        public int PerfectWaveLevel { get; set; } = 100;
        public int PerfectWaveTime { get; set; } = 10;
    }

    public class MissConfig
    {
        public bool Enable { get; set; } = true;
        public string MissWave { get; set; } = "经典";
        public int MissWaveLevel { get; set; } = 100;
        public int MissWaveTime { get; set; } = 10;
    }


    public class ModMain : MelonMod
    {
        private static WebSocketClient _webSocketClient;

        public static WebSocketClient WebSocketClient => _webSocketClient;


        private float _timer;
        private const float Interval = 1.0f; // Interval in seconds

        private MelonPreferences_Category ModConfigCategory;
        private MelonPreferences_Entry<string> ModConfigEntry;
        private static ModConfigData ModConfig;
        private static MelonPreferences_Entry<bool> UnlockAllMasterEntry;


        public override void OnInitializeMelon()
        {
            

            ModConfigCategory = MelonPreferences.CreateCategory("MuseDash_DGLAB_XM");
            ModConfigEntry = ModConfigCategory.CreateEntry("ModConfig", string.Empty);
            UnlockAllMasterEntry = ModConfigCategory.CreateEntry("UnlockAllMaster", false);

            // 读取并反序列化配置项
            if (string.IsNullOrEmpty(ModConfigEntry.Value))
            {
                ModConfig = new ModConfigData();
                SaveConfig();
            }
            else
            {
                ModConfig = JsonSerializer.Deserialize<ModConfigData>(ModConfigEntry.Value, GetJsonSerializerOptions());

            }

            //连接ws
            _webSocketClient = new WebSocketClient(ModConfig.Host);
            // Initialize WebSocket connection
            Task.Run(async () => await _webSocketClient.ConnectAsync());

            Melon<ModMain>.Logger.Msg("Mod Load!");
        }
        //public override void OnUpdate()
        //{
        //    _timer += UnityEngine.Time.deltaTime;

        //    if (_timer >= Interval)
        //    {
        //        _timer = 0; // Reset the timer

        //        // Example of sending a message every second
        //        if (_webSocketClient.IsConnected)
        //        {
        //            Task.Run(async () => await _webSocketClient.SendMessageAsync("{\"type\":\"update\",\"value\":\"example\"}"));
        //        }
        //        else
        //        {

        //            Melon<ModMain>.Logger.Msg("Reconn");
        //            //Task.Run(async () => await _webSocketClient.ConnectAsync());
        //        }
        //    }
        //}

        private JsonSerializerOptions GetJsonSerializerOptions()
        {
            return new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 禁用 Unicode 转义
            };
        }

        // 序列化并保存配置
        private void SaveConfig()
        {
            ModConfigEntry.Value = JsonSerializer.Serialize(ModConfig, GetJsonSerializerOptions());
            MelonPreferences.Save();
        }




        //音符结算分数时调用
        [HarmonyPatch(typeof(Il2CppAssets.Scripts.GameCore.Managers.StatisticsManager))]
        [HarmonyPatch("OnGetScore")]
        [HarmonyPatch(new Type[] {typeof(int), typeof(int), typeof(int), typeof(string), typeof(int),typeof(int), typeof(Side), typeof(int), typeof(int), typeof(int),typeof(int), typeof(int)})]
        private static class OnGetScorePatch
        {
            private static void Postfix(Il2CppAssets.Scripts.GameCore.Managers.StatisticsManager __instance, int __0, int __1, int judge, string __3, int __4, int __5, Il2CppAssets.Scripts.Common.Side __6, int __7, int __8, int __9, int __10, int __11)
            {
                //modconfig类是我的json配置类
                if (ModConfig.NoteRate.Enable == false)
                {
                    //总开关是false 啥都不执行
                    return;
                }
                //Melon<ModMain>.Logger.Msg(judge.ToString());
                if (judge == 0 && ModConfig.NoteRate.EnablePass)
                {
                    var json = string.Format("{{\"cmd\":\"{0}\",\"pattern_name\":\"{1}\",\"intensity\":{2},\"ticks\":{3}}}", "set_pattern", ModConfig.NoteRate.PassWave, ModConfig.NoteRate.PassWaveLevel, ModConfig.NoteRate.PassWaveTime);
                    Task.Run(async () => await ModMain.WebSocketClient.SendMessageAsync(json));
                }
                else if (judge == 3 && ModConfig.NoteRate.EnableGreat)
                {
                    var json = string.Format("{{\"cmd\":\"{0}\",\"pattern_name\":\"{1}\",\"intensity\":{2},\"ticks\":{3}}}", "set_pattern", ModConfig.NoteRate.GreateWave, ModConfig.NoteRate.GreateWaveLevel, ModConfig.NoteRate.GreateWaveTime);
                    Task.Run(async () => await ModMain.WebSocketClient.SendMessageAsync(json));
                }
                else if (judge == 4 && ModConfig.NoteRate.EnablePerfect)
                {
                    var json = string.Format("{{\"cmd\":\"{0}\",\"pattern_name\":\"{1}\",\"intensity\":{2},\"ticks\":{3}}}", "set_pattern", ModConfig.NoteRate.PerfectWave, ModConfig.NoteRate.PerfectWaveLevel, ModConfig.NoteRate.PerfectWaveTime);
                    Task.Run(async () => await ModMain.WebSocketClient.SendMessageAsync(json));
                }
            }
        }

        [HarmonyPatch(typeof(Il2CppAssets.Scripts.GameCore.HostComponent.BattleRoleAttributeComponent))]
        [HarmonyPatch("Miss")]
        [HarmonyPatch(new Type[] {  })]
        private static class Patch
        {
            private static void Postfix()
            {
                if (ModConfig.Miss.Enable == false)
                {
                    return;
                }

                var json = string.Format("{{\"cmd\":\"{0}\",\"pattern_name\":\"{1}\",\"intensity\":{2},\"ticks\":{3}}}", "set_pattern", ModConfig.Miss.MissWave, ModConfig.Miss.MissWaveLevel, ModConfig.Miss.MissWaveTime);
                Task.Run(async () => await ModMain.WebSocketClient.SendMessageAsync(json));
            }
        }



        [HarmonyPatch(typeof(Il2CppAssets.Scripts.Database.DataHelper))]
        [HarmonyPatch("get_isUnlockAllMaster")]
        [HarmonyPatch(new Type[] { })]
        private static class UnlockPatch
        {

            private static bool Prefix(ref bool __result)
            {
                if (UnlockAllMasterEntry.Value == true){
                    __result = true;
                    //Melon<ModMain>.Logger.Msg("UnlockAllMaster!");
                    // 返回 false 以跳过原始方法
                    return false;
                }
                else
                {
                    return true;
                }
                
                
            }
        }


        [HarmonyPatch(typeof(SteamApps))]
        [HarmonyPatch("BIsDlcInstalled")]
        [HarmonyPatch(new Type[] { typeof(AppId_t) })]
        private static class DLCPatch
        {

            static bool Prefix(ref bool __result, AppId_t appID)
            {

                if (UnlockAllMasterEntry.Value == true)
                {
                 
                    __result = true;
                    //Melon<ModMain>.Logger.Msg("UnlockAllMaster!");
                    // 返回 false 以跳过原始方法
                    return false;
                }
                else
                {
                    return true;
                }


            }
        }




    }
}
