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

            itemFactors = new Dictionary<string, double>() {
                { "Ingot Uranium",    1.0 },
                { "Ingot Silver",     1.0 },
                { "Ingot Magnesium",  2.0 },
                { "Ingot Silicon",    4.0 },
                { "Ingot Platinum",    4.0 },
                { "Ingot Gold",       4.0 },
                { "Ingot Cobalt",     8.0 },
                { "Ingot Nickel",     16.0 },
                //{ "Iron Ingot",       64.0 },
            };


            
            clear();
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
                    println("Invalid command, available commands are:\n GET, CLEAR, HANGAR, INTEGRITY-INIT");
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

            foreach (var inventory in inventories) {
                foreach (var item in inventory.GetInventory(0).GetItems()) {
                    var name = getName(item);
                    
                    if (itemCounts.ContainsKey(name))
                        itemCounts[name] += item.Amount;
                    else
                        itemCounts.Add(name, item.Amount);
                }
            }

            clear();
            println("Begin");

            foreach (var elem in itemCounts) {
                println($"{ elem.Key }: amount: { elem.Value }");
            }

            /*foreach (var elem in itemFactors) {
                if (itemCounts.ContainsKey(elem.Key))
                    println($"{ elem.Key }: factor: { elem.Value }, prio: { getPrio(itemCounts, elem) }");
            }*/

            println("End");

            manageItems(itemCounts);
            //updateHydrogenFuel(itemCounts);
            //trackBlocksCheck();
        }

        private void updateHydrogenFuel(Dictionary<string, VRage.MyFixedPoint> itemCounts) {
            double iceCount = (double)itemCounts["Ice"];
            double iceMass = iceCount * ORE_MASS_PER_COUNT;

            GridTerminalSystem.GetBlocksOfType<IMyGasTank>(blocks);
            GridTerminalSystem.SearchBlocksOfName("Gydrogen Tank", blocks);
            var sum = 0.0;
            foreach (var block in blocks) {
                var tank = block as IMyGasTank;
                sum += tank.FilledRatio;
            }
            var totalFilledRatio = sum / blocks.Count;
            //TODO: do stuff with totalFilledRatio
        }

        private void manageItems(Dictionary<string, VRage.MyFixedPoint> itemCounts) {
            var containers = new List<IMyCargoContainer>();

            GridTerminalSystem.GetBlocksOfType(containers, container => container.CubeGrid == Me.CubeGrid);

            //Only do one sorting operation per update()
            switch (itemTickTock) {
                case 0:
                    updateRefineries(containers, itemCounts);
                    break;
                case 1:
                    //Clear all connectors
                    GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(blocks);
                    foreach (var block in blocks) {
                        var connector = block as IMyShipConnector;
                        dumpItems(connector.GetInventory(), containers);
                    }
                    break;
                case 2:
                    //Collect refined ore from refineries
                    GridTerminalSystem.GetBlocksOfType<IMyRefinery>(blocks);
                    foreach (var block in blocks) {
                        var refinery = block as IMyRefinery;
                        var refineryOutput = refinery.GetInventory(1);
                        dumpItems(refineryOutput, containers);
                    }
                    break;
                case 3:
                    //Collect all ore from docked ships
                    GridTerminalSystem.GetBlocksOfType(blocks, block => block.HasInventory && block.CubeGrid != Me.CubeGrid);
                    foreach (var block in blocks) {
                        for (int i = 0; i < block.InventoryCount; i++) {
                            var inventory = block.GetInventory(i);
                            dumpItems(inventory, containers, name => {
                                return name.Contains("Ore") && !name.Contains("Ice");
                            });
                        }
                    }
                    break;
                default:
                    break;
            }
            itemTickTock = (itemTickTock + 1) % 4;
        }

        void sortInventory(List<IMyCargoContainer> containers) {

            
        }
        
        /// <summary>
        /// Updates refineries to produce what is currently most needed
        /// </summary>
        /// <param name="itemCounts"></param>
        private void updateRefineries(List<IMyCargoContainer> containers, Dictionary<string, VRage.MyFixedPoint> itemCounts) {
            var sorted = itemFactors
                .Where(elem => {
                    var oreName = getOreFromIngotName(elem.Key);
                    //It is only possible to refine a material that exists
                    return itemCounts.ContainsKey(oreName)/* && itemCounts[oreName] > 10*/;
                })
                .OrderBy(elem => 
                    getPrio(itemCounts, elem)
                );

            string oreToRefine = "None";
            double bestPrio = double.MaxValue;

            foreach(var elem in itemFactors) {
                var oreName = getOreFromIngotName(elem.Key);
                println($"{ elem.Key }: Ore name: { oreName }");
                if (itemCounts.ContainsKey(oreName)) {
                    if (getPrio(itemCounts, elem) < bestPrio) {
                        //println($"{ elem.Key }: prio: { getPrio(itemCounts, elem) } < { oreToRefine }: prio: { bestPrio }");
                        oreToRefine = oreName;
                        bestPrio = getPrio(itemCounts, elem);
                    }
                    //println($"{ elem.Key }: factor: { elem.Value }, prio: { getPrio(itemCounts, elem) }");
                }
            }


            /*foreach (var elem in sorted) {
                println($"{ elem.Key }: factor: { elem.Value }, prio: { getPrio(itemCounts, elem) }");
            }*/

            //var ingotName = sorted.First().Key;

            //println($"Refined ore: { ingotName }, ");
            //var oreToRefine = getOreFromIngotName(ingotName);

            GridTerminalSystem.GetBlocksOfType<IMyRefinery>(blocks, refinery => {
                var refineryInput = refinery.GetInventory(0).GetItems();

                //Check if the refinery is doing anything it shouldn't
                return refineryInput.Count == 0 || 
                    refineryInput.Exists(item => getName(item) != oreToRefine);
            });

            

            var resourceAavailable = itemCounts[oreToRefine];
            var resourcePerRefinery = (double)resourceAavailable / blocks.Count;

            foreach (var block in blocks) {
                var rebeliousRefinery = block as IMyRefinery;
                var refInventory = rebeliousRefinery.GetInventory(0);

                dumpItems(refInventory, containers, (name => name != oreToRefine));
                var amountAlreadyInRef = (double)getItemAmount(refInventory, name => name == oreToRefine);

                //If refinery has too much resource, dump it
                if (amountAlreadyInRef > resourcePerRefinery)
                    dumpItems(refInventory, containers, (name => name == oreToRefine));
                else//If refinery does not have enough, request more
                    requestItems(refInventory, oreToRefine, resourcePerRefinery - amountAlreadyInRef, containers);
            }

            println($"Ore to refine: { oreToRefine }, ");
        }

        /// <summary>
        /// Gets ore name from corresponding ingot name. For example "Iron Ingot" becomes "Iron"
        /// </summary>
        /// <param name="ingotName">Ingot name</param>
        /// <returns>Ore name</returns>
        private string getOreFromIngotName(string ingotName)
        {
            return "Ore " + ingotName.Split(' ')[1];
        }

        /// <summary>
        /// Get priority value representing how high the priority currently is to refine a specific metal
        ///
        /// Low value means high priority
        /// </summary>
        /// <param name="itemCounts"></param>
        /// <param name="factorElem"></param>
        /// <returns>Low value means high priority</returns>
        private double getPrio(Dictionary<string, VRage.MyFixedPoint> itemCounts, KeyValuePair<string, double> factorElem)
        {
            var itemName = factorElem.Key;
            var factor = factorElem.Value;
            var itemCount = itemCounts.ContainsKey(factorElem.Key) ?
                            itemCounts[itemName] : 0;
            return (double)itemCount / factor;
        }


        /// <summary>
        /// Dump items from dst into specified destinations
        /// Items will dumped to destinations[0] first and if destinations[0] gets filled up, destinations[1] will be dumped into and so on.
        /// 
        /// Note: filter() == true is for what items to MOVE
        /// </summary>
        /// <param name="src">Inventory to dump</param>
        /// <param name="destinations">List of inventories to put items into</param>
        /// <param name="filter">Filter deciding what items to move</param>
        /// <param name="amountToDump">Total amount of items to dump</param>
        /// <returns>Amount of items matching filter still left</returns>
        private double dumpItems(IMyInventory src, List<IMyCargoContainer> destinations, Func<string, bool> filter = null, double amountToDump = double.MaxValue) {
            double amountDumped = 0;
            for (int i = 0; i < destinations.Count && src.CurrentVolume != 0 && amountDumped <= amountToDump; i++) {
                var containerInventory = destinations[i].GetInventory(0);
                amountDumped += moveItems(src, containerInventory, filter, amountToDump);
            }

            return (double)getItemAmount(src, filter);
        }

        /// <summary>
        /// Request items from any of the specified sources into the destination
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="requestedItem">Name of item to move</param>
        /// <param name="requestedAmount">How many items to move</param>
        /// <param name="sources">list of inventorys to take items from</param>
        /// <returns>Total amount of items moved</returns>
        private double requestItems(IMyInventory dst, string requestedItem, double requestedAmount, List<IMyCargoContainer> sources)
        {
            double transferredAmount = 0.0;

            for (int i = 0; i < sources.Count; i++) {
                transferredAmount += moveItems(sources[i].GetInventory(), dst, (name => name == requestedItem), requestedAmount);
            }
            return transferredAmount;
        }

        /// <summary>
        /// Request items from any of the specified sources into the destination
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="filter">Decides what items to move</param>
        /// <param name="requestedAmount">How many items to move</param>
        /// <param name="sources">list of inventorys to take items from</param>
        /// <returns>Total amount of items moved</returns>
        private double requestItems(IMyInventory dst, Func<string, bool> filter, double requestedAmount, List<IMyCargoContainer> sources) {
            double transferredAmount = 0.0;

            for (int i = 0; i < sources.Count; i++) {
                transferredAmount += moveItems(sources[i].GetInventory(), dst, filter, requestedAmount);
            }
            return transferredAmount;
        }

        
        /// <summary>
        /// Tries to move a total of 'requestedAmount' of items matching fitler from src to dst.
        /// 
        /// EXAMPLE: moveItems(src, dst, name => name.Contains("Plate"), 10);
        /// where src contains 5 'Steel Plates' and 5 'Interior Plate' will return 10.0
        /// </summary>
        /// <param name="src">Source</param>
        /// <param name="dst">Destination</param>
        /// <param name="filter">Filter used to determine what items to move</param>
        /// <param name="requestedAmount">Amount of items to move</param>
        /// <returns>Totalamount of actually moved items</returns>
        private double moveItems(IMyInventory src, IMyInventory dst, Func<string, bool> filter = null, double requestedAmount = double.MaxValue)
        {
            double totalAmountTransferred = 0.0;

            var srcItems = src.GetItems();
            for (int i = 0; i < srcItems.Count && !dst.IsFull;) {
                string name = getName(srcItems[i]);
                if (filter != null && !filter(name)) {
                    i++;
                    continue;
                }

                var amountAvailable = srcItems[i].Amount;
                var amountToTransfer = (VRage.MyFixedPoint)min((double)amountAvailable, requestedAmount - totalAmountTransferred);
                if (requestedAmount <= totalAmountTransferred)
                    return totalAmountTransferred;

                var amountBefore = getItemAmount(dst, filter);

                var b = src.TransferItemTo(dst, i, null, true, amountToTransfer);

                var amountAfter = getItemAmount(dst, filter);
                var transferedAmount = (double)(amountAfter - amountBefore);

                //Destination is full
                if (transferedAmount < 0.01)
                    return totalAmountTransferred;

                totalAmountTransferred += transferedAmount;
            }
            return totalAmountTransferred;
        }

        /// <summary>
        /// Get the amount of items matching the filter, if no filter is specified the total amount is returned
        /// </summary>
        /// <param name="inventory"></param>
        /// <param name="filter"></param>
        /// <returns>Amount of items matching filter if specified</returns>
        private VRage.MyFixedPoint getItemAmount(IMyInventory inventory, Func<string, bool> filter = null)
        {
            VRage.MyFixedPoint amount = 0;

            var items = inventory.GetItems();

            if (filter == null) {
                foreach (var item in items) {
                    amount += item.Amount;
                }
            }
            else {
                foreach (var item in items.Where(item => filter(getName(item)))) {
                    amount += item.Amount;
                }
            }
            return amount;
        }


        /// <summary>
        /// Check wether item is an ore
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool isOre(IMyInventoryItem item, bool includeIce, bool includeStone) {
            var name = getName(item);
            return isOre(name, includeIce, includeStone);
        }

        private string getName(IMyInventoryItem item) {
            return item.Content.TypeId.ToString().Substring(16) + ' ' + item.Content.SubtypeName;
        }

        /// <summary>
        /// Check wether item is an ore
        /// </summary>
        /// <param name="itemName">Name of potential ore</param>
        /// <returns></returns>
        private bool isOre(string itemName, bool includeIce, bool includeStone) {
            HashSet<string> ores = new HashSet<string>(){
                "Uranium",
                "Silver",
                "Magnesium",
                "Silicon",
                "Platinum",
                "Gold",
                "Cobalt",
                "Nickel",
                "Iron"
            };

            if (includeIce && itemName == "Ice")
                return true;
            if (includeStone && itemName == "Stone")
                return true;

            return ores.Contains(itemName);
        }

        /// <summary>
        /// Check wether item is a refined ore
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool isRefined(IMyInventoryItem item) {
            var name = getName(item);
            HashSet<string> names = new HashSet<string>(){
                "Uranium Ingot",
                "Silver Ingot",
                "Magnesium Powder",
                "Silicon Wafer",
                "Platinum Ingot",
                "Gold Ingot",
                "Cobalt Ingot",
                "Nickel Ingot",
                "Iron Ingot"
            };
            return names.Contains(name);
        }

        private double min(double a, double b) {
            return a < b ? a : b;
        }

        private void getAllNames() {
            var inventories = blocks;
            GridTerminalSystem.GetBlocksOfType(inventories, block => block.HasInventory);

            HashSet<string> items = new HashSet<string>();

            foreach (var inventory in inventories)
            {
                foreach (var item in inventory.GetInventory(0).GetItems())
                {
                    var name = getName(item);
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

        const double ORE_MASS_PER_COUNT = 2.7;


        string msg;

        //Scratch container for blocks
        List<IMyTerminalBlock> blocks;

        Dictionary<string, double> itemFactors;
        List<IMyTerminalBlock> trackedBlocks;
        string trackedBlockStatus;

        int itemTickTock = 0;
    }
}