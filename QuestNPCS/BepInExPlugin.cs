using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using QuestFramework;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;
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
        
        public static QuestNPC.NPCText currentText = new QuestNPC.NPCText();
        public static bool showQuestAcceptWindow;
        public static bool respondedToQuest = false;
        public static QuestData nextQuest = new QuestData();
        public static QuestData finishedQuest = new QuestData();

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
                UnityEngine.Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {   
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

            //Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            CustomItem weaRara = new CustomItem("SwordIron", true);
            weaRara.ItemPrefab.name = "weaRara";
            ItemManager.Instance.AddItem(weaRara);
        }
        public static void GetQuests()
        {
            NPCQuestDict.Clear();

            if (Directory.Exists(Path.Combine(Assembly.GetExecutingAssembly().Location, "Quests")) && Directory.GetFiles(Path.Combine(Assembly.GetExecutingAssembly().Location, "Quests")).Length > 0)
            {
                List<NPCQuestData> fqdList = new List<NPCQuestData>();
                foreach (string file in Directory.GetFiles(Path.Combine(Assembly.GetExecutingAssembly().Location, "Quests")))
                {
                    try
                    {
                        fqdList.Add(JsonUtility.FromJson<NPCQuestData>(File.ReadAllText(file)));
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            string[] lines = File.ReadAllLines(file);
                            foreach (string line in lines)
                            {
                                fqdList.Add(JsonUtility.FromJson<NPCQuestData>(line));
                            }
                        }
                        catch
                        {
                            Dbgl($"Error reading quests from {file}:\n\n{ex}");
                        }
                    }
                }
                if (fqdList.Count > 0)
                {
                    for (int i = 0; i < fqdList.Count; i++)
                    {
                        NPCQuestData fqd = fqdList[i];
                        fqd.ID = typeof(BepInExPlugin).Namespace + "_" + fqd.ID;
                        if (fqd.amount <= 0)
                        {
                            fqd.amount = UnityEngine.Random.Range(minAmount.Value, maxAmount.Value);
                            fqd.rewardAmount *= fqd.amount;
                        }
                        //fqd.rewardAmount = Mathf.RoundToInt(fqd.rewardAmount * (1 + (rewardFluctuation.Value * 2 * Random.value - rewardFluctuation.Value)));
                        if (fqd.type == QuestType.Fetch)
                        {
                            try
                            {
                                fqd.thing = ObjectDB.instance.GetItemPrefab(fqd.thing).GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
                            }
                            catch { }
                        }
                        else if (fqd.type == QuestType.Kill)
                        {
                            try
                            {
                                fqd.thing = ZNetScene.instance.GetPrefab(fqd.thing).GetComponent<Character>().m_name;
                            }
                            catch { }
                        }


                        NPCQuestDict[fqd.ID] = fqd;
                        Dbgl($"Added quest {fqd.ID}");
                    }
                }
            }
            if (NPCQuestDict.Count == 0)
            {
                possibleKillList = ((Dictionary<int, GameObject>)AccessTools.Field(typeof(ZNetScene), "m_namedPrefabs").GetValue(ZNetScene.instance)).Values.ToList().FindAll(g => g.GetComponent<MonsterAI>() || g.GetComponent<AnimalAI>());

                possibleFetchList.Clear();
                var fetchList = ObjectDB.instance.m_items.FindAll(g => g.GetComponent<ItemDrop>() && (g.GetComponent<ItemDrop>().m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Material || g.GetComponent<ItemDrop>().m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable));
                foreach (GameObject go in fetchList)
                {
                    int value = GetItemValue(go.GetComponent<ItemDrop>());
                    if (value > 0)
                        possibleFetchList.Add(new GameObjectReward(go, value));
                }

                possibleBuildList.Clear();

                ItemDrop hammer = ObjectDB.instance.GetItemPrefab("Hammer")?.GetComponent<ItemDrop>();
                var buildList = new List<GameObject>(hammer.m_itemData.m_shared.m_buildPieces.m_pieces);
                ItemDrop hoe = ObjectDB.instance.GetItemPrefab("Hoe")?.GetComponent<ItemDrop>();
                buildList.AddRange(hoe.m_itemData.m_shared.m_buildPieces.m_pieces);
                foreach (GameObject go in buildList)
                {
                    var reqs = go.GetComponent<Piece>().m_resources;
                    int value = 0;
                    foreach (var req in reqs)
                    {
                        value += GetItemValue(req.m_resItem) * req.m_amount;
                    }
                    if (value > 0)
                        possibleBuildList.Add(new GameObjectReward(go, value));
                }

                Dbgl($"got {possibleFetchList.Count} possible fetch items, {possibleKillList.Count} possible kill items, and {possibleBuildList.Count} possible build items");

            }
        }

        private static int GetItemValue(ItemDrop itemDrop)
        {
            int value = itemDrop.m_itemData.m_shared.m_value;
            if (Chainloader.PluginInfos.ContainsKey("Menthus.bepinex.plugins.BetterTrader"))
            {
                var key = Chainloader.PluginInfos["Menthus.bepinex.plugins.BetterTrader"].Instance.Config.Keys.ToList().Find(c => c.Section.StartsWith("C_Items") && c.Section.EndsWith("." + itemDrop.name) && c.Key == "Sellable");
                if (key != null && (bool)Chainloader.PluginInfos["Menthus.bepinex.plugins.BetterTrader"].Instance.Config[key].BoxedValue)
                {
                    key = Chainloader.PluginInfos["Menthus.bepinex.plugins.BetterTrader"].Instance.Config.Keys.ToList().Find(c => c.Section == key.Section && c.Key == "Sell Price");
                    value = (int)Chainloader.PluginInfos["Menthus.bepinex.plugins.BetterTrader"].Instance.Config[key].BoxedValue;
                    //Dbgl($"Got Better Trader price for {itemDrop.m_itemData.m_shared.m_name} of {value}; section {key.Section} key {key.Key}");
                }
            }
            /*if (value <= 0 && randomWorthlessItemValue.Value > 0)
            {
                value = randomWorthlessItemValue.Value;
            }*/
            return value;
        }

        public static QuestData MakeRandomQuest()
        {
            Dbgl("Making random custom quest");
            int idx = UnityEngine.Random.Range(0, NPCQuestDict.Count);
            return MakeQuestData(NPCQuestDict[NPCQuestDict.Keys.ToList()[idx]]);
        }
        public static QuestData MakeQuestData(NPCQuestData fqd)
        {
            QuestData qd = new QuestData()
            {
                name = fqd.questName.Replace("{rewardAmount}", $"{fqd.rewardAmount}").Replace("{amount}", $"{fqd.amount}").Replace("{progress}", "0"),
                desc = fqd.questDesc.Replace("{rewardAmount}", $"{fqd.rewardAmount}").Replace("{amount}", $"{fqd.amount}").Replace("{progress}", "0"),
                ID = fqd.ID,
                currentStage = "StageOne",
                data = new Dictionary<string, object>() {
                    { "qname", fqd.questName },
                    { "qdesc", fqd.questDesc },
                    { "sname", fqd.stageName },
                    { "sdesc", fqd.stageDesc },
                    { "oname", fqd.objectiveName },
                    { "odesc", fqd.objectiveDesc },
                    { "progress", 0 },
                    { "type", fqd.type },
                    { "thing", fqd.thing },
                    { "amount", fqd.amount },
                    { "rewardAmount", fqd.rewardAmount },
                    { "rewardName", fqd.rewardName },
                },
                questStages = new Dictionary<string, QuestStage>()
                    {
                        {
                            "StageOne",
                            new QuestStage(){
                                name = fqd.stageName.Replace("{rewardAmount}", $"{fqd.rewardAmount}").Replace("{amount}", $"{fqd.amount}").Replace("{progress}", "0"),
                                desc = fqd.stageDesc.Replace("{rewardAmount}", $"{fqd.rewardAmount}").Replace("{amount}", $"{fqd.amount}").Replace("{progress}", "0"),
                                ID = "StageOne",
                                objectives = new Dictionary<string, QuestObjective>()
                                {
                                    {
                                        "ObjectiveOne",
                                        new QuestObjective()
                                        {
                                            name = fqd.objectiveName.Replace("{rewardAmount}", $"{fqd.rewardAmount}").Replace("{amount}", $"{fqd.amount}").Replace("{progress}", "0"),
                                            desc = fqd.objectiveDesc.Replace("{rewardAmount}", $"{fqd.rewardAmount}").Replace("{amount}", $"{fqd.amount}").Replace("{progress}", "0"),
                                            ID = "ObjectiveOne",
                                        }
                                    }
                                }
                            }
                        },
                        {
                            "StageTwo",
                            new QuestStage(){
                                ID = "StageTwo",
                                objectives = new Dictionary<string, QuestObjective>()
                                {
                                    {
                                        "ObjectiveOne",
                                        new QuestObjective()
                                        {
                                            name = returnString.Value,
                                            desc = returnDescString.Value,
                                            ID = "ObjectiveOne",
                                        }
                                    }
                                }
                            }
                        }
                    }
            };
            return qd;
        }
        private static void DeclineQuest()
        {
            Dbgl("Declining quest");
            currentText.m_topic = questDeclinedDialogue.Value;
            respondedToQuest = true;
            questDialogueTransform.gameObject.SetActive(false);
        }

        private static void AcceptQuest()
        {
            Dbgl("Accepting quest");
            currentText.m_topic = questAcceptedDialogue.Value;
            QuestFrameworkAPI.AddQuest(nextQuest);
            respondedToQuest = true;
            questDialogueTransform.gameObject.SetActive(false);
            AdjustFetchQuests();
        }

        public static void AdvanceKillQuests(Character character)
        {
            var dict = QuestFrameworkAPI.GetCurrentQuests();
            string[] keys = dict.Keys.ToArray();
            foreach (string key in keys)
            {
                QuestData qd = dict[key];
                if (qd.ID.StartsWith(typeof(BepInExPlugin).Namespace) && (QuestType)qd.data["type"] == QuestType.Kill && qd.currentStage == "StageOne")
                {
                    if ((string)qd.data["thing"] == character.m_name)
                    {
                        qd.data["progress"] = (int)qd.data["progress"] + 1;

                        UpdateQuestProgress(ref qd);

                        if ((int)qd.data["progress"] >= (int)qd.data["amount"])
                            qd.currentStage = "StageTwo";
                        QuestFrameworkAPI.AddQuest(qd, true);
                    }
                }
            }
        }

        public static void AdvanceBuildQuests(Piece piece)
        {
            var dict = QuestFrameworkAPI.GetCurrentQuests();
            string[] keys = dict.Keys.ToArray();
            foreach (string key in keys)
            {
                QuestData qd = dict[key];
                if (qd.ID.StartsWith(typeof(BepInExPlugin).Namespace) && (QuestType)qd.data["type"] == QuestType.Build && qd.currentStage == "StageOne")
                {
                    if ((string)qd.data["thing"] == Utils.GetPrefabName(piece.gameObject))
                    {
                        qd.data["progress"] = (int)qd.data["progress"] + 1;

                        UpdateQuestProgress(ref qd);

                        if ((int)qd.data["progress"] >= (int)qd.data["amount"])
                        {
                            qd.currentStage = "StageTwo";
                        }
                        QuestFrameworkAPI.AddQuest(qd, true);
                    }
                }
            }
        }

        public static void AdjustFetchQuests()
        {
            var dict = QuestFrameworkAPI.GetCurrentQuests();
            string[] keys = dict.Keys.ToArray();
            foreach (string key in keys)
            {
                QuestData qd = dict[key];
                if (qd.ID.StartsWith(typeof(BepInExPlugin).Namespace) && (QuestType)qd.data["type"] == QuestType.Fetch)
                {
                    int amount = Player.m_localPlayer.GetInventory().CountItems((string)qd.data["thing"]);
                    if (amount != (int)qd.data["progress"])
                    {
                        qd.data["progress"] = amount;

                        UpdateQuestProgress(ref qd);

                        if ((int)qd.data["progress"] >= (int)qd.data["amount"])
                            qd.currentStage = "StageTwo";
                        else
                        {
                            qd.currentStage = "StageOne";
                            if (finishedQuest != null && finishedQuest.ID == qd.ID)
                                finishedQuest = null;
                        }

                        QuestFrameworkAPI.AddQuest(qd, true);
                    }
                }
            }
        }

        private static void UpdateQuestProgress(ref QuestData qd)
        {
            qd.name = ((string)qd.data["qname"]).Replace("{rewardAmount}", $"{qd.data["rewardAmount"]}").Replace("{amount}", $"{qd.data["amount"]}").Replace("{progress}", $"{qd.data["progress"]}");
            qd.desc = ((string)qd.data["qdesc"]).Replace("{rewardAmount}", $"{qd.data["rewardAmount"]}").Replace("{amount}", $"{qd.data["amount"]}").Replace("{progress}", $"{qd.data["progress"]}");
            qd.questStages["StageOne"].name = ((string)qd.data["sname"]).Replace("{rewardAmount}", $"{qd.data["rewardAmount"]}").Replace("{amount}", $"{qd.data["amount"]}").Replace("{progress}", $"{qd.data["progress"]}");
            qd.questStages["StageOne"].desc = ((string)qd.data["sdesc"]).Replace("{rewardAmount}", $"{qd.data["rewardAmount"]}").Replace("{amount}", $"{qd.data["amount"]}").Replace("{progress}", $"{qd.data["progress"]}");
            qd.questStages["StageOne"].objectives["ObjectiveOne"].name = ((string)qd.data["oname"]).Replace("{rewardAmount}", $"{qd.data["rewardAmount"]}").Replace("{amount}", $"{qd.data["amount"]}").Replace("{progress}", $"{qd.data["progress"]}");
            qd.questStages["StageOne"].objectives["ObjectiveOne"].desc = ((string)qd.data["odesc"]).Replace("{rewardAmount}", $"{qd.data["rewardAmount"]}").Replace("{amount}", $"{qd.data["amount"]}").Replace("{progress}", $"{qd.data["progress"]}");
        }

        public static void FulfillQuest(QuestData qd)
        {
            if (qd.currentStage == "StageTwo")
            {
                if ((string)qd.data["rewardName"] == "Gold")
                    qd.data["rewardName"] = "Coins";
                ItemDrop itemDrop = ObjectDB.instance.GetItemPrefab((string)qd.data["rewardName"])?.GetComponent<ItemDrop>();
                if (!itemDrop)
                {
                    currentText.m_topic = "There is something wrong with your quest...";
                    Dbgl($"Error getting reward {qd.data["rewardName"]}");
                    return;
                }

                int emptySlots = Player.m_localPlayer.GetInventory().GetWidth() * Player.m_localPlayer.GetInventory().GetHeight() - Player.m_localPlayer.GetInventory().GetAllItems().Count;
                int stackSpace = (int)AccessTools.Method(typeof(Inventory), "FindFreeStackSpace").Invoke(Player.m_localPlayer.GetInventory(), new object[] { itemDrop.m_itemData.m_shared.m_name });
                stackSpace += emptySlots * itemDrop.m_itemData.m_shared.m_maxStackSize;
                if (stackSpace < (int)qd.data["rewardAmount"])
                {
                    currentText.m_topic = noRoomDialogue.Value;
                    Dbgl($"No room for reward! {stackSpace} {(int)qd.data["rewardAmount"]}");
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, "No room for reward!", 0, null);
                    return;
                }
                if ((QuestType)qd.data["type"] == QuestType.Fetch)
                {
                    if (Player.m_localPlayer.GetInventory().CountItems((string)qd.data["thing"]) < (int)qd.data["amount"])
                    {
                        currentText.m_topic = "It seems you have not completed your quest...";
                        Dbgl($"not enough to complete quest!");
                        if (finishedQuest != null && finishedQuest.ID == qd.ID)
                            finishedQuest = null;
                        AdjustFetchQuests();
                        return;
                    }
                    Player.m_localPlayer.GetInventory().RemoveItem((string)qd.data["thing"], (int)qd.data["amount"]);
                }

                Player.m_localPlayer.GetInventory().AddItem(itemDrop.gameObject.name, (int)qd.data["rewardAmount"], itemDrop.m_itemData.m_quality, itemDrop.m_itemData.m_variant, 0L, "");
                Player.m_localPlayer.ShowPickupMessage(itemDrop.m_itemData, (int)qd.data["rewardAmount"]);
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, completeString.Value, 0, null);
                QuestFrameworkAPI.RemoveQuest(qd.ID);
                finishedQuest = null;
                currentText.m_topic = completedDialogue.Value;
                Dbgl($"Quest {qd.ID} completed");
            }
            else
            {
                Dbgl($"Quest {qd.ID} isn't ready to complete");

            }
        }
        
    }
}
