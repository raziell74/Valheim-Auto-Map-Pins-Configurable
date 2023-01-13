﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Utilities;
using AMP_Configurable.Patches;

namespace AMP_Configurable
{
    [BepInPlugin("amped.mod.auto_map_pins", "AMPED - Auto Map Pins Enhanced", "1.3.0")]
    [BepInProcess("valheim.exe")]
    public class Mod : BaseUnityPlugin {
        //***CONFIG ENTRIES***//
        //***GENERAL***//
        public static ManualLogSource Log;
        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<float> pinOverlapDistance;
        public static ConfigEntry<int> pinRange;
        public static ConfigEntry<bool> hideAllLabels;
        public static ConfigEntry<string> hidePinLabels;
        public static ConfigEntry<string> savePinTypes;
        public static ConfigEntry<string> customPinSizes;
        public static ConfigEntry<bool> loggingEnabled;
        
        //***ORES***//
        public static ConfigEntry<bool> oresEnabled;
        public static ConfigEntry<bool> oresLoggingEnabled;

        //***PICKABLES***//
        public static ConfigEntry<bool> pickablesEnabled;
        public static ConfigEntry<bool> pickablesLoggingEnabled;

        //***LOCATIONS***//
        public static ConfigEntry<bool> locsEnabled;
        public static ConfigEntry<bool> locsLoggingEnabled;

        //***SPAWNERS***//
        public static ConfigEntry<bool> spwnsEnabled;
        public static ConfigEntry<bool> spwnsLoggingEnabled;

        //***CREATURES***//
        public static ConfigEntry<bool> creaturesEnabled;
        public static ConfigEntry<bool> creaturesLoggingEnabled;

        //***PUBLIC VARIABLES***//
        public static PinConfig.PinConfig pinTypes;
        public static List<Minimap.PinData> autoPins;
        public static List<Minimap.PinData> savedPins;
        public static List<Minimap.PinData> pinRemList;
        public static List<Vector3> addedPinLocs;
        public static List<Vector3> dupPinLocs;
        public static List<string> filteredPins;
        public static Vector3 position;
        public static Dictionary<Vector3, PinConfig.PinType> pinItems = new Dictionary<Vector3, PinConfig.PinType>();
        public static Dictionary<Vector3, Minimap.PinData> remPinDict = new Dictionary<Vector3, Minimap.PinData>();
        public static bool hasMoved = false;
        public static bool checkingPins = false;
        public static string currEnv = "";

        private void Awake() {
            Log = Logger;
            
            /** General Config **/
            nexusID = Config.Bind("General", "NexusID", 744, "Nexus mod ID for updates");
            modEnabled = Config.Bind("General", "Enabled", true, "Enable this mod");
            loggingEnabled = Config.Bind("General", "Enable Logging", true, "Enable Logging");
            pinOverlapDistance = Config.Bind<float>("General", "PinOverlapDistance", 10, "Distance around pins to prevent overlapping of similar pins");
            pinRange = Config.Bind("General", "Pin Range", 150, "Sets the range that pins will appear on the mini-map. Lower value means you need to be closer to set pin.\nMin 5\nMax 150\nRecommended 50-75");
            if (pinRange.Value < 5) pinRange.Value = 5;
            if (pinRange.Value > 150) pinRange.Value = 150;
            hideAllLabels = Config.Bind("General", "Hide All Labels", false, "Hide all pin labels.\n*THIS WILL OVERRIDE THE INDIVIDUAL SETTINGS*");
            hidePinLabels = Config.Bind("General", "Hide Pin Label", "", "Hide individual pin type labels.\nValue should be a comma seperated list of pin types.");
            
            string defaultSavePinTypes = "Crypt,Troll Cave,Sunken Crypt,Frost Cave,Infested Mine";
            savePinTypes = Config.Bind("General", "Save Pin Types", defaultSavePinTypes, "These Pin Types will persist on the map after the player as left the area.\nValue should be a comma seperated list of pin types.");
            customPinSizes = Config.Bind("General", "Pin Size Override", "", "Customize the size of individual pins.\nValue should be a comma seperated list of pin types each with a colon ':' and a size.\nExample: Berries:20,Troll Cave:25");
            
            //***ORES***//
            oresEnabled = Config.Bind("Resources", "Enabled", true, "Enable/Disable pins for\nOres, Trees, and other destructable resource nodes");
            oresLoggingEnabled = Config.Bind("Resources", "Enable Logging", true, "Log object id and position of each destructable resource node in range of the player.\nUsed to get object Ids to assign to pin types");
            
            //***PICKABLES***//
            pickablesEnabled = Config.Bind("Pickables", "Enabled", true, "Enable/Disable pins for\nOres, Trees, and other destructable resource nodes");
            pickablesLoggingEnabled = Config.Bind("Pickables", "Enable Logging", true, "Log object id and position of each destructable resource node in range of the player.\nUsed to get object Ids to assign to pin types");
            
            //***LOCATIONS***//
            locsEnabled = Config.Bind("Locations", "Enabled", true, "Enable/Disable pins for\nOres, Trees, and other destructable resource nodes");
            locsLoggingEnabled = Config.Bind("Locations", "Enable Logging", true, "Log object id and position of each destructable resource node in range of the player.\nUsed to get object Ids to assign to pin types");
            
            //***SPAWNERS***//
            spwnsEnabled = Config.Bind("Spawners", "Enabled", true, "Enable/Disable pins for\nOres, Trees, and other destructable resource nodes");
            spwnsLoggingEnabled = Config.Bind("Spawners", "Enable Logging", true, "Log object id and position of each destructable resource node in range of the player.\nUsed to get object Ids to assign to pin types");
            
            //***CREATURES***//
            creaturesEnabled = Config.Bind("Creatures", "Enabled", true, "Enable/Disable pins for\nOres, Trees, and other destructable resource nodes");
            creaturesLoggingEnabled = Config.Bind("Creatures", "Enable Logging", true, "Log object id and position of each destructable resource node in range of the player.\nUsed to get object Ids to assign to pin types");

            /** Load Pin Type Config from JSON file **/
            pinTypes = ResourceUtils.LoadPinConfig("amp_pin_types.json");

            if (!modEnabled.Value)
                enabled = false;
            else
            {
                new Harmony("materousapps.mods.automappins_configurable").PatchAll();
                
                Harmony.CreateAndPatchAll(typeof(Minimap_Patch), "materousapps.mods.automappins_configurable");
                Harmony.CreateAndPatchAll(typeof(DestructiblePatchSpawn), "materousapps.mods.automappins_configurable");
                Harmony.CreateAndPatchAll(typeof(PickablePatchSpawn), "materousapps.mods.automappins_configurable");
                Harmony.CreateAndPatchAll(typeof(LocationPatchSpawn), "materousapps.mods.automappins_configurable");
                Harmony.CreateAndPatchAll(typeof(SpawnAreaPatchSpawn), "materousapps.mods.automappins_configurable");
                Harmony.CreateAndPatchAll(typeof(MineRockPatchSpawn), "materousapps.mods.automappins_configurable");
                Harmony.CreateAndPatchAll(typeof(Player_Patches), "materousapps.mods.automappins_configurable");

                addedPinLocs = new List<Vector3>();
                dupPinLocs = new List<Vector3>();
                autoPins = new List<Minimap.PinData>();
                pinRemList = new List<Minimap.PinData>();

                /** 
                 * @TODO Preserve these lists in physical conf files 
                 **/
                savedPins = new List<Minimap.PinData>();
                filteredPins = new List<string>();

                Assets.Init();          
            }
        }

        public static bool SimilarPinExists(
          Vector3 pos,
          Minimap.PinType type,
          List<Minimap.PinData> pins,
          string aName,
          Sprite aIcon,
          out Minimap.PinData match)
        {
            foreach (Minimap.PinData pin in pins)
            {
                if(pos == pin.m_pos)
                {
                    match = pin;
                    return true;
                }
                else
                //Log.LogInfo(string.Format("[AMP] Checking Distance between Pins {0} & {1}: {2}", aName, pin.m_name, (double)Utils.DistanceXZ(pos, pin.m_pos)));
                if ((double)Utils.DistanceXZ(pos, pin.m_pos) < pinOverlapDistance.Value
                    && type == pin.m_type 
                    && (aName == pin.m_name || aIcon == pin.m_icon))
                {
                    //Log.LogInfo(string.Format("[AMP] Duplicate pins for {0} found", aName));
                    match = pin;
                    return true;
                }
            }
            match = null;
            return false;
        }

        public static void checkPins(Vector3 charPos)
        {
            //Log.LogInfo(string.Format("[AMP] checkingPins? {0}", checkingPins));
            if (checkingPins)
                return;

            foreach (KeyValuePair<Vector3, PinConfig.PinType> kvp in pinItems)
            {
                checkingPins = true;
                if (Vector3.Distance(charPos, kvp.Key) < pinRange.Value)
                {
                    if (!addedPinLocs.Contains(kvp.Key) && !dupPinLocs.Contains(kvp.Key))
                    {
                        PinnedObject.pinOb(kvp.Value, kvp.Key);
                    }
                }
                else if (Vector3.Distance(charPos, kvp.Key) > pinRange.Value)
                {
                    foreach (Minimap.PinData tempPin in autoPins)
                    {

                        if (!remPinDict.ContainsKey(kvp.Key) && !tempPin.m_save)
                        {
                            //Log.LogInfo(string.Format("Adding {0} at {1} to removal List", kvp.Value, kvp.Key));
                            remPinDict.Add(kvp.Key, tempPin);
                            pinRemList.Add(tempPin);
                        }
                    }
                }
            }
            checkingPins = false;

            if (pinRemList != null)
            {
                foreach (Minimap.PinData tempRemPin in pinRemList)
                {
                    if (Vector3.Distance(charPos, tempRemPin.m_pos) < pinRange.Value)
                        return;

                    Minimap.instance.RemovePin(tempRemPin);
                    autoPins.Remove(tempRemPin);
                    addedPinLocs.Remove(tempRemPin.m_pos);
                    remPinDict.Remove(tempRemPin.m_pos);
                    //Log.LogInfo(string.Format("Removed Pin {0} at {1}", tempRemPin.m_name, tempRemPin.m_pos));
                }
            }
            pinRemList.Clear();
            
        }

        public static Minimap.PinData GetNearestPin(Vector3 pos, float radius, List<Minimap.PinData> pins)
        {

            Minimap.PinData pinData = null;
            float num1 = 999999f;
            foreach (Minimap.PinData pin in pins)
            {
                float num2 = Utils.DistanceXZ(pos, pin.m_pos);
                if (num2 < radius && (num2 < num1 || pinData == null))
                {
                    pinData = pin;
                    num1 = num2;
                    //Log.LogInfo(string.Format("[AMP] Nearest Pin Type = {0} at {1}", pinData.m_type, pinData.m_pos));
                }
            }
            return pinData;
        }

        public static void FilterPins()
        {
            savedPins.ForEach(pin => pin.m_uiElement?.gameObject.SetActive(ShouldPinRender(pin)));
        }

        private static bool ShouldPinRender(Minimap.PinData pin)
        {
            if (filteredPins == null || filteredPins.Count == 0)
                return true;

            if (!filteredPins.Contains(pin.m_type.ToString()))
                return true;

            return false;
            //return (uint)((IEnumerable<string>)filteredPins).Count(filter => !pin.m_type.ToString().ToLower().Replace(' ', '_').Contains(filter)) > 0U;
        }

        public Mod()
        {
            return;
        }
    }

    internal class PinnedObject : MonoBehaviour
    {
        public static Minimap.PinData pin;
        public static bool aSave = false;
        public static bool showName = false;
        public static float pinSize = 20;
        public static Sprite aIcon;
        public static string aName = "";
        public static int pType;

        public static void pinOb(PinConfig.PinType tempPin, Vector3 aPos)
        {
            if (Mod.currEnv == "Crypt" || Mod.currEnv == "SunkenCrypt" || Mod.currEnv == "FrostCaves" || Mod.currEnv == "InfectedMine")
                return;

            if (tempPin != null)
            {
                loadData(tempPin);

                //don't show filtered pins.
                if (Mod.filteredPins.Contains(pType.ToString()))
                    return;

                if (Mod.autoPins.Count > 0)
                {
                    if (Mod.SimilarPinExists(aPos, (Minimap.PinType)pType, Mod.autoPins, aName, aIcon, out Minimap.PinData _))
                    {
                        Mod.dupPinLocs.Add(aPos);
                        return;
                    }
                }

                //Mod.Log.LogInfo(string.Format("[AMP] Checking Distance between Player position {0} & {1} position {2}: {3}", GameCamera.instance.transform.position, aName, aPos, Vector3.Distance(GameCamera.instance.transform.position, aPos)));
                if (Mod.hideAllLabels.Value)
                    showName = false;

                if (showName)
                {
                    pin = Minimap.instance.AddPin(aPos, (Minimap.PinType)pType, aName, aSave, false);
                }
                else
                {
                    pin = Minimap.instance.AddPin(aPos, (Minimap.PinType)pType, string.Empty, aSave, false);
                }
                //Mod.Log.LogInfo(string.Format("[AMP] Added Pin {0} at {1}", pin.m_name, pin.m_pos));

                if (aIcon)
                    pin.m_icon = aIcon;

                pin.m_worldSize = pinSize;
                pin.m_save = aSave;
                
                //Mod.Log.LogInfo(string.Format("[AMP] Tracking: {0} at {1} {2} {3}", aName, aPos.x, aPos.y, aPos.z));
                if(!Mod.autoPins.Contains(pin))
                    Mod.autoPins.Add(pin);
                if(!Mod.addedPinLocs.Contains(aPos))
                    Mod.addedPinLocs.Add(aPos);

                if (pin.m_save && !Mod.savedPins.Contains(pin))
                    Mod.savedPins.Add(pin);
            }
        }

        private void Update()
        {
            
        }

        private void OnDestroy()
        {
            if (pin == null || Minimap.instance == null)
                return;

            loadData(null, pin.m_type.ToString());
            //Mod.Log.LogInfo(string.Format("OnDestroy Called for Type {0} at {1}", pin.m_type, pin.m_pos));
            //Mod.Log.LogInfo(string.Format("[AMP] Save Pin {0}? = {1}.", pin.m_name, aSave));

            if (aSave || pin.m_save)
            {
                //Mod.Log.LogInfo(string.Format("[AMP] Leaving Pin {0} on map.", pin.m_name));
                return;
            }
            if (Vector3.Distance(pin.m_pos, Player_Patches.currPos) < Mod.pinRange.Value)
                return;

            //Minimap.instance.RemovePin(pin);
            //Mod.autoPins.Remove(pin);
            //Mod.addedPinLocs.Remove(pin.m_pos);
            //Mod.dupPinLocs.Remove(pin.m_pos);
            //Mod.pinItems.Remove(pin.m_pos);
        }

        public static void loadData(PinConfig.PinType pin = null, string m_type = null)
        {
            /** loadData called with minimap pin type data **/
            if(pin == null && m_type != null) {
                pType = Int32.Parse(m_type);
                foreach (PinConfig.PinType pinType in Mod.pinTypes.resources) 
                {
                    if(pinType.type == pType) 
                    {
                        aName = pinType.label;
                        pType = pinType.type;
                        aSave = Mod.savePinTypes.Value.Split(',').Contains(pinType.label);
                        aIcon = pinType.sprite;
                        showName = !Mod.hidePinLabels.Value.Split(',').Contains(pinType.label);
                        pinSize = pinType.size;
                        break;
                    }
                }
                
                return;
            }
            
            aName = pin.label;
            pType = pin.type;
            aSave = Mod.savePinTypes.Value.Split(',').Contains(pin.label);
            aIcon = pin.sprite;
            showName = !Mod.hidePinLabels.Value.Split(',').Contains(pin.label);
            pinSize = pin.size;
    }

        public PinnedObject()
        {
            return;
        }
    }

    public class Assets
    {
        public static void Init() {
            foreach (PinConfig.PinType pinType in Mod.pinTypes.resources)
                pinType.sprite = LoadSprite(pinType.icon);
            
            foreach (PinConfig.PinType pinType in Mod.pinTypes.pickables)
                pinType.sprite = LoadSprite(pinType.icon);

            foreach (PinConfig.PinType pinType in Mod.pinTypes.locations)
                pinType.sprite = LoadSprite(pinType.icon);

            foreach (PinConfig.PinType pinType in Mod.pinTypes.spawners)
                pinType.sprite = LoadSprite(pinType.icon);

            foreach (PinConfig.PinType pinType in Mod.pinTypes.creatures)
                pinType.sprite = LoadSprite(pinType.icon);
        }

        internal static Texture2D LoadTexture(byte[] file)
        {
            if (((IEnumerable<byte>)file).Count<byte>() > 0)
            {
                Texture2D texture2D = new Texture2D(2, 2);
                if (ImageConversion.LoadImage(texture2D, file))
                {
                    return texture2D;
                }
            }
            return (Texture2D)null;
        }

        public static Sprite LoadSprite(string iconPath, float PixelsPerUnit = 50f) {
            Texture2D SpriteTexture = LoadTexture(ResourceUtils.GetResource(iconPath));
            return SpriteTexture? Sprite.Create(SpriteTexture, new Rect(0.0f, 0.0f, SpriteTexture.width, SpriteTexture.height), new Vector2(0.0f, 0.0f), PixelsPerUnit) : (Sprite)null;
        }
    }
}
