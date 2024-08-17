using Advanced_Combat_Tracker;
using System;
using System.Windows.Forms;
using RearFlankChecker.Util;
using System.Media;
using System.Reflection;
using System.Linq;

namespace RearFlankChecker
{
    public class PluginMain : IActPluginV1
    {
        public DataManager Settings { get; private set; }
        public ACTTabControl ACTTabControl { get; private set; }
        public AttackMissView AttackMissView { get; private set; }
        private SoundPlayer soundPlayer;
        private String actionEffecNetworkAbility = "ActionEffect 15:";
        private dynamic dataRepository;
        private CombatActionChecker checker;
        private const int TIMESTAMP_OFFSET = 15;

        public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
        {
            AttackMissView = new AttackMissView(this);
            ACTTabControl = new ACTTabControl(this);
            pluginScreenSpace.Text = Assembly.GetExecutingAssembly().GetName().Name;
            pluginScreenSpace.Controls.Add(ACTTabControl);
            ACTTabControl.InitializeSettings();

            Settings = new DataManager(this);
            Settings.Load();

            ACTTabControl.Show();

            String path = ResourceLocator.findResourcePath("resources/wav/miss.wav");
            if(path != null)
                soundPlayer = new SoundPlayer(path);

            var xivPlugin = GetPluginData("FFXIV_ACT_Plugin.dll");

            if (xivPlugin != null)
            {
                dataRepository = xivPlugin.pluginObj.GetType().GetProperty("DataRepository").GetValue(xivPlugin.pluginObj, null);
            }
            if (dataRepository == null)
            {
                ActGlobals.oFormActMain.WriteDebugLog($"RFC: Cannot get DataRepository from ACT Plugin {xivPlugin} {dataRepository}");
            }

            ActGlobals.oFormActMain.OnCombatStart += CombatStarted;
            ActGlobals.oFormActMain.OnLogLineRead += OnLogLineRead;
            checker = new CombatActionChecker();
        }
        private Advanced_Combat_Tracker.ActPluginData GetPluginData(string pluginName)
        {
            foreach (var plugin in Advanced_Combat_Tracker.ActGlobals.oFormActMain.ActPlugins)
            {
                if (!plugin.cbEnabled.Checked)
                    continue;
                if (plugin.pluginFile.Name == pluginName)
                    return plugin;
            }
            return null;
        }

        public void DeInitPlugin()
        {
            ActGlobals.oFormActMain.OnCombatStart -= CombatStarted;
            ActGlobals.oFormActMain.OnLogLineRead -= OnLogLineRead;

            if (Settings != null)
                Settings.Save();

            if (AttackMissView != null)
                AttackMissView.Close();

            if (soundPlayer != null)
            {
                soundPlayer.Dispose();
            }
        }

        private void CombatStarted(bool isImport, CombatToggleEventArgs encounterInfo)
        {
            AttackMissView.Reset();
        }

        private void OnLogLineRead(bool isImport, LogLineEventArgs logInfo)
        {
            String logLine = logInfo.logLine;

            uint playerID = 0;

            if (dataRepository != null) {
                playerID = dataRepository.GetType().GetMethod("GetCurrentPlayerID").Invoke(dataRepository, null);
            }
            if (playerID == 0)
            {
                ActGlobals.oFormActMain.WriteDebugLog("RFC: Cannot get the current player ID");
                return;
            }
            string playerHex = playerID.ToString("X");

            // 0x15: NetworkAbility
            // Reference: https://github.com/OverlayPlugin/cactbot/blob/main/docs/LogGuide.md#line-21-0x15-networkability
            if (logLine.Length < TIMESTAMP_OFFSET + actionEffecNetworkAbility.Length || !logLine.Substring(TIMESTAMP_OFFSET, actionEffecNetworkAbility.Length).Equals(actionEffecNetworkAbility))
                return;


            string[] lineDatas = logLine.Substring(TIMESTAMP_OFFSET).Split(':');


            if (lineDatas.Length < 8)
            {
                return;
            }

            if (!lineDatas[1].Equals(playerHex))
            {
                return;
            }
            UInt32 skillID = UInt32.Parse(lineDatas[3], System.Globalization.NumberStyles.HexNumber);
            UInt32 effect1 = UInt32.Parse(lineDatas[7], System.Globalization.NumberStyles.HexNumber);
            string skillName = lineDatas[4];
            UInt32 bonusPercent = effect1 >> 24;

            if (!checker.JudgeFlankOrRearSkill(skillID, bonusPercent, skillName))
            {
                AttackMissView.CountUp(lineDatas[4]);

                if (soundPlayer != null && ACTTabControl.IsSoundEnable())
                {
                    soundPlayer.Play();
                }
            }
        }
    }

}
