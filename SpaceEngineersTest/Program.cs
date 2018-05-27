using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // In order to add a new utility class, right-click on your project, 
        // select 'New' then 'Add Item...'. Now find the 'Space Engineers'
        // category under 'Visual C# Items' on the left hand side, and select
        // 'Utility Class' in the main area. Name it in the box below, and
        // press OK. This utility class will be merged in with your code when
        // deploying your final script.
        //
        // You can also simply create a new utility class manually, you don't
        // have to use the template if you don't want to. Just do so the first
        // time to see what a utility class looks like.

        public Program()
        {
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script. 
            //     
            // The constructor is optional and can be removed if not
            // needed.
            // 
            // It's recommended to set RuntimeInfo.UpdateFrequency 
            // here, which will allow your script to run itself without a 
            // timer block.


            //Run every 100th tick
            Runtime.UpdateFrequency = UpdateFrequency.Update100 | UpdateFrequency.Once;


            blocks = new List<IMyTerminalBlock>();

            preferredItemCounts = new Dictionary<string, VRage.MyFixedPoint>() {
                { "Gold",       1000 },
                { "Silver",     1000 },
                { "Magnesium",  1000 },
                { "Silicon",    1000 },
                { "Iron",       1000 },
                { "Nickel",     1000 },
                { "Ice",        1000 },
                { "Stone",      1000 },
                { "Cobalt",     1000 },
                { "Uranium",    1000 },
            };


            indicators = new Dictionary<string, IMyInteriorLight>();
            foreach (var elem in preferredItemCounts) {
                var item = elem.Key;

                var block = GridTerminalSystem.GetBlockWithName("Indicator " + item) as IMyInteriorLight;
                indicators.Add(item, block);
            }
            clear();
            suppressWarnings = false;
            trackedBlocks = new List<IMyTerminalBlock>();
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (argument.Length == 0) {
                update();
                flush();
                return;
            }

            var args = argument.ToLower().Split(new char[]{ ' ' });
            
            switch(args[0]) {
                case "get":
                    if (args.Length < 2) {
                        println($"Get command takes atleast one argument, { args.Length - 1 } given!");
                    }
                    else {
                        cmdGet(args);
                    }
                    break;
                case "clear":
                    clear();
                    break;
                case "warnings":
                    if (args.Length != 2) {
                        println($"Get command takes one argument, { args.Length - 1 } given!");
                    }
                    else {
                        cmdWarnings(args[1]);
                    }
                    break;

                case "hangar":
                    if (args.Length < 2) {
                        println($"GET command takes atleast one argument, { args.Length - 1 } given!");
                    }
                    else {
                        cmdHangar(args);
                    }
                    break;
                case "integrity-init":
                    if (args.Length < 2)
                    {
                        println($"INTEGRITY-INIT command takes no arguments, { args.Length - 1 } given!");
                    }
                    else
                    {
                        trackBlocksReset();
                    }
                    break;

                default:
                    println("Invalid command, available commands are:\n GET, CLEAR, WARNINGS");
                    break;
            }
            flush();
        }

        private void cmdGet(string[] args) {
            switch(args[1]) {
                case "mass":
                    if (args.Length != 2) {
                        println("GET MASS does not take any arguments!");
                        return;
                    }
                    getMass();
                    break;

                case "info":
                    if (args.Length < 3) {
                        println($"GET INFO expects atleast one argument, { args.Length - 2 } given!");
                        return;
                    }
                    getBlockInfo(args);
                    break;

                case "pressure":
                    if (args.Length != 2)
                    {
                        println($"GET PRESSURE takes no arguments, { args.Length - 2 } given!");
                        return;
                    }
                    getPressure();
                    break;

                default:
                    break;
            }
        }

        private void getPressure()
        {
            GridTerminalSystem.GetBlocksOfType<IMyAirVent>(blocks);

            foreach (var block in blocks)
            {
                var vent = block as IMyAirVent;

                println(vent.CustomName + (vent.CanPressurize ? ": Airtight" : ": Leak"));
            }
        }

        void cmdWarnings(string arg) {
            switch(arg) {
                case "on":
                case "ON":
                case "On":
                    suppressWarnings = false;
                    break;
                case "off":
                case "OFF":
                case "Off":
                    suppressWarnings = true;
                    break;

                default:
                    println("Warnings command only takes arguments ON or OFF");
                    break;
            }
        }

        void cmdHangar(string[] args) {
            switch (args[1]) {
                case "doors":
                    if (args.Length < 4) {
                        println($"HANGAR DOORS takes atleast two arguments, { args.Length - 2 } given!");
                    }
                    else {
                        controlDoors(args);
                    }
                    break;

                default:
                    println($"Invalid argument to hangar command: { args[1] }");
                    break;
            }
        }

        private void controlDoors(string[] args) {

            if (args[2] == "left" || args[2] == "right") {
                var dir = args[2].First().ToString().ToUpper() + args[2].Substring(1);



                if (args[3] == "open" || args[3] == "close")
                    controlHangarDoors(dir, args[3] == "open");
                else if (args[3] == "toggle")
                    controlHangarDoors(dir, null);
                else
                    println($"HANGAR DOORS LEFT|RIGHT only takes OPEN, CLOSE or TOGGLE as arguments, given: { args[3] }");
            }
            else {
                println($"Invalid argument to hangar command: { args[1] }");
            }
            
        }

        private void controlHangarDoors(string dir, bool? open) {
            var hangarDoors = GridTerminalSystem.GetBlockGroupWithName("HangarDoor " + dir);
            hangarDoors.GetBlocks(blocks);

            var thisDoor = blocks[0] as IMyAirtightHangarDoor;
            bool beforeThisOpen = thisDoor.Status == DoorStatus.Open || thisDoor.Status == DoorStatus.Opening;

            foreach (var block in blocks)
            {
                var door = (block as IMyAirtightHangarDoor);
                if (open == null)
                {
                    door.ToggleDoor();
                }
                else if (open == true)
                    door.OpenDoor();
                else
                    door.CloseDoor();
            }


            var thisOpen = (open == true) || (open == null && !beforeThisOpen);
            var thisJustOpened = (!beforeThisOpen && (open != false));

            doorLightHelper(dir, thisOpen);

            var invdir = dir == "Left" ? "Right" : "Left";
            var otherHangarDoors = GridTerminalSystem.GetBlockGroupWithName("HangarDoor " + invdir);
            hangarDoors.GetBlocks(blocks);
            var otherDoor = (blocks[0] as IMyAirtightHangarDoor);

            var otherOpen = otherDoor.Status == DoorStatus.Open || otherDoor.Status == DoorStatus.Opening;


            hangarPressureIndicator(thisOpen || otherOpen, /*thisJustOpened*/ true && !otherOpen);

        }

        private void doorLightHelper(string dir, bool on) {
            var lights = GridTerminalSystem.GetBlockGroupWithName("CornerLight Hangar " + dir);
            lights.GetBlocks(blocks);

            foreach (var light in blocks)
            {
                (light as IMyFunctionalBlock).Enabled = on;
            }
        }

        private void hangarPressureIndicator(bool on, bool justOpened)
        {
            var group = GridTerminalSystem.GetBlockGroupWithName("Hangar Pressurized Indicator");
            if (group != null)
            {
                group.GetBlocks(blocks);

                foreach (var indicator in blocks)
                {
                    if (indicator is IMyInteriorLight)
                    {
                        if (on)
                        {
                            var light = indicator as IMyInteriorLight;
                            light.SetValueColor("Color", new Color(255, 0, 0));
                            light.SetValue("Blink Interval", (float)1);
                        }
                        else
                        {
                            var light = indicator as IMyInteriorLight;
                            light.SetValueColor("Color", new Color(255, 255, 255));
                            light.SetValue("Blink Interval", (float)0);
                        }
                    }
                    else
                    {
                        (indicator as IMyFunctionalBlock).Enabled = on;
                    }
                }
            }
            else
            {
                println("'Hangar Pressurized Indicator' group not found!");
            }
        }
        private void getMass() {
            GridTerminalSystem.GetBlocks(blocks);
            float mass = 0.0f;

            foreach (var block in blocks) {
                mass += block.Mass;
            }
            if (mass > 1000)
                println($"Mass: { mass / 1000 }T");
            else
                println($"Mass: { mass } kg");
        }

        private void getBlockInfo(string[] arg)
        {
            
            var searchPattern = String.Join(" ", arg.Skip(2));
            
            GridTerminalSystem.SearchBlocksOfName(searchPattern, blocks);
            
            if (blocks.Count == 0) {
                println($"No blocks matching { searchPattern }");
                return;
            }

            Dictionary<string, uint> blockCount = new Dictionary<string, uint>();
            Dictionary<string, float> blockMass = new Dictionary<string, float>();


            foreach (var block in blocks) {
                var name = block.CustomName;
                if (blockCount.ContainsKey(name)) {
                    blockCount[name]++;
                    blockMass[name] += block.Mass;
                } else {
                    blockCount.Add(name, 1);
                    blockMass.Add(name, block.Mass);
                }
            }

            
            foreach (var block in blockCount.Keys) {
                println($"{ block }: { blockCount[block] }pcs, { blockMass[block] }kg total");
            }
        }

        private void update() {
            var inventories = blocks;
            GridTerminalSystem.GetBlocksOfType(inventories, block => block.HasInventory);


            Dictionary<string, VRage.MyFixedPoint> itemCounts = new Dictionary<string, VRage.MyFixedPoint>();

            foreach (var inventory in inventories)
            {
                foreach (var item in inventory.GetInventory(0).GetItems())
                {
                    var name = item.Content.SubtypeName;
                    
                    if (itemCounts.ContainsKey(name))
                        itemCounts[name] += item.Amount;
                    else
                        itemCounts.Add(name, item.Amount);
                }
            }

            updateIndicators(itemCounts);

            trackBlocksCheck();
        }

        private void updateIndicators(Dictionary<string, VRage.MyFixedPoint> itemCounts) {

            foreach (var elem in preferredItemCounts) {
                var item = elem.Key;
                var preferredItemCount = elem.Value;

                //Indicates whether the current item count is within the preferred limit
                var withinLimit = itemCounts.ContainsKey(item) && itemCounts[item] >= preferredItemCount;

                var indicatorName = "Indicator " + item;

                if (!indicators.ContainsKey(item))
                    if(!suppressWarnings) println($"Warning could not find light '{ indicatorName }', (not in list)");
                else if (indicators[item] == null)
                    if (!suppressWarnings) println($"Warning could not find light '{ indicatorName }', (was NULL)");
                else
                    indicators[item].Enabled = withinLimit;
            }

        }

        private void getAllNames() {
            var inventories = blocks;
            GridTerminalSystem.GetBlocksOfType(inventories, block => block.HasInventory);

            HashSet<string> items = new HashSet<string>();

            foreach (var inventory in inventories)
            {
                foreach (var item in inventory.GetInventory(0).GetItems())
                {
                    var name = item.Content.SubtypeName;
                    items.Add(name);
                }
            }

            foreach (var item in items) {
                println(item);
            }
        }

        //TODO: expose this as a command
        private void trackBlocksReset()
        {
            GridTerminalSystem.GetBlocks(trackedBlocks);
        }

        //TODO: call this from update()
        private void trackBlocksCheck()
        {
            GridTerminalSystem.GetBlocksOfType(blocks, block => block.IsFunctional);
            var damagedBlocks = trackedBlocks.Except(blocks);
            //TODO: do stuff with damaged blocks
            trackedBlockStatus = "";
            foreach (var block in damagedBlocks)
            {
                trackedBlockStatus += $"  { block.CustomName }\n";
            }
            if (trackedBlockStatus != "")
            {
                trackedBlockStatus = "Damaged blocks:\n" + trackedBlockStatus;
            }
        }

        private void print(string text) {
            msg += text;
        }

        void println(string line) {
            msg += line + '\n';
            var loggLcd = GridTerminalSystem.GetBlockWithName("Corner LCD Top InsideCenter") as IMyTextPanel;
            loggLcd.WritePublicText(line + '\n', true);
        }

        void clear() {
            msg = "";
            var loggLcd = GridTerminalSystem.GetBlockWithName("Corner LCD Top InsideCenter") as IMyTextPanel;
            loggLcd.WritePublicText("", false);
        }

        void flush() {
            Echo(msg);
            Echo(trackedBlockStatus);
            var lcd = GridTerminalSystem.GetBlockWithName("LCD block damage") as IMyTextPanel;
            if(lcd != null)
                lcd.WritePublicText(trackedBlockStatus, false);
        }

        string msg;
        bool suppressWarnings;

        //Scratch container for blocks
        List<IMyTerminalBlock> blocks;

        Dictionary<string, VRage.MyFixedPoint> preferredItemCounts;
        Dictionary<string, IMyInteriorLight> indicators;
        List<IMyTerminalBlock> trackedBlocks;
        string trackedBlockStatus;
    }
}