using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using QuestFramework;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Jotunn;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;

namespace QuestNPCS
{
    [BepInPlugin("Valkyrie.QuestNPCS", "Quest NPC's", "0.0.1")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<string> modKey;
        public static ConfigEntry<bool> modEnabled;
        //public static ConfigEntry<int> nexusID;

        public static ConfigEntry<int> maxQuests;
        public static ConfigEntry<int> minAmount;
        public static ConfigEntry<int> maxAmount;
        public static ConfigEntry<int> maxReward;

        public static ConfigEntry<string> questDeclinedDialogue;
        public static ConfigEntry<string> questAcceptedDialogue;
        public static ConfigEntry<string> noRoomDialogue;
        public static ConfigEntry<string> completedDialogue;
        public static ConfigEntry<string> haveRewardDialogue;
        public static ConfigEntry<string> haveQuestDialogue;
        public static ConfigEntry<string> declineButtonText;
        public static ConfigEntry<string> acceptButtonText;
        public static ConfigEntry<string> questNameString;
        public static ConfigEntry<string> questDescString;
        public static ConfigEntry<string> returnString;
        public static ConfigEntry<string> returnDescString;
        public static ConfigEntry<string> startString;
        public static ConfigEntry<string> completeString;
        public static ConfigEntry<string> killQuestString;
        public static ConfigEntry<string> fetchQuestString;


        //private static BepInExPlugin context;

        public static double lastCheckTime = 0;
        public static QuestNPC.NPCText currentText = new QuestNPC.NPCText();
        public static bool showQuestAcceptWindow;
        public static bool respondedToQuest = false;
        public static QuestData nextQuest;
        public static QuestData finishedQuest;

        public static GUIStyle titleStyle;
        public static GUIStyle subTitleStyle;
        public static GUIStyle descStyle;
        public static float windowWidth = 400;
        public static float windowHeight = 300;
        public static int windowID = 1890175404;
        public static Transform questDialogueTransform;
        public static Transform questDialogueSubtitleTransform;
        public static Transform questDialogueTitleTransform;

        public static Dictionary<string, NPCQuestData> NPCQuestDict = new Dictionary<string, NPCQuestData>();
        public static List<GameObject> possibleKillList = new List<GameObject>();
        public static List<GameObjectReward> possibleFetchList = new List<GameObjectReward>();
        public static List<GameObjectReward> possibleBuildList = new List<GameObjectReward>();
        public enum QuestType
        {
            Fetch,
            Kill,
            Build
        }

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            //context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            //nexusID = Config.Bind<int>("General", "NexusID", 1588, "Nexus mod ID for updates");

            maxQuests = Config.Bind<int>("Options", "MaxQuests", 1, "Number of quests to allow at once.");

            minAmount = Config.Bind<int>("Options", "MinAmount", 3, "Minimum number of things in quests without specified amounts.");
            maxAmount = Config.Bind<int>("Options", "MaxAmount", 10, "Maximum number of things in quest without specified amounts.");

            questAcceptedDialogue = Config.Bind<string>("Text", "QuestAcceptedDialogue", "May you be victorious in your endeavor...", "Hugin's dialogue when accepting a quest.");
            questDeclinedDialogue = Config.Bind<string>("Text", "QuestDeclinedDialogue", "Perhaps next time...", "Hugin's dialogue when declining a quest.");
            noRoomDialogue = Config.Bind<string>("Text", "NoRoomDialogue", "You have no room for your reward...", "Hugin's dialogue if you have no room for your reward.");
            completedDialogue = Config.Bind<string>("Text", "CompletedDialogue", "Well done... until next we meet!", "Hugin's dialogue after completing quest.");
            haveRewardDialogue = Config.Bind<string>("Text", "HaveRewardDialogue", "Come take your reward...", "Hugin's dialogue when there's a completed quest.");
            haveQuestDialogue = Config.Bind<string>("Text", "HaveQuestDialogue", "I have a quest for you...", "Hugin's dialogue when there's a quest.");
            declineButtonText = Config.Bind<string>("Text", "DeclineButtonText", "Decline", "Text for button to decline quest.");
            acceptButtonText = Config.Bind<string>("Text", "AcceptButtonText", "Accept", "Text for button to accept quest.");
            questNameString = Config.Bind<string>("Text", "QuestString", "Hugin's Request", "Main quest header string.");
            returnString = Config.Bind<string>("Text", "ReturnString", "Talk to Hugin.", "Return objective.");
            returnDescString = Config.Bind<string>("Text", "ReturnDescString", "Talk to the raven for your reward.", "Return objective description.");
            startString = Config.Bind<string>("Text", "StartString", "Quest Started.", "HUD message on quest start.");
            completeString = Config.Bind<string>("Text", "CompleteString", "Quest Completed.", "HUD message on quest completion.");
            killQuestString = Config.Bind<string>("Text", "KillQuestString", "Kill Quest", "Kill quest string.");
            fetchQuestString = Config.Bind<string>("Text", "FetchQuestString", "Fetch Quest", "Fetch quest string.");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            CustomItem weaRara = new CustomItem("SwordIron", true);
            weaRara.ItemPrefab.name = "weaRara";
            ItemManager.Instance.AddItem(weaRara);
        }

    }
}
