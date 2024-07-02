using ACulinaryArtillery.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.GameContent;
using Vintagestory.Server;
using static System.Reflection.Metadata.BlobBuilder;
using static Vintagestory.Client.NoObf.ClientPlatformWindows;

namespace ACulinaryArtillery.GUI
{

    public class InventoryRecipeCreator : InventoryBasePlayer
    {
        ItemSlot[] slots;
        public ItemSlot[] Slots { get { return slots; } }
        public InventoryRecipeCreator(string inventoryID, ICoreAPI api) : base(inventoryID, api)
        {
            if (className == null)
            {
                className = "inventoryRecipeCreator";
            }
            //slots 0-5 = ingredients
            //slot 6 = output
            slots = GenEmptySlots(7);
            //this.LateInitialize(inventoryID, api);

        }
        public InventoryRecipeCreator(string className, string playerUID, ICoreAPI api)
            : base(className, playerUID, api)
        {
            if (className == null)
            {
                base.className = "inventoryRecipeCreator";
            }
            slots = GenEmptySlots(7);
        }
        public override ItemSlot this[int slotId]
        {
            get
            {
                if (slotId < 0 || slotId >= Count) return null;
                return slots[slotId];
            }
            set
            {
                if (slotId < 0 || slotId >= Count) throw new ArgumentOutOfRangeException(nameof(slotId));
                if (value == null) throw new ArgumentNullException(nameof(value));
                slots[slotId] = value;
            }
        }

        public override int Count { get { return 7; } }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            slots = SlotsFromTreeAttributes(tree, slots);
        }
        protected override ItemSlot NewSlot(int i)
        {
            return new ItemSlotWatertight(this, 100);
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            SlotsToTreeAttributes(slots, tree);
        }
    }
    public class GuiRecipeCreator : GuiDialog
    {
        public InventoryRecipeCreator Inventory;
        enum recType
        {
            Simmering,
            Kneading
        }
        recType OutPutRecipeType = recType.Kneading;
        string outputJson = "";
        string OutPutRecipeTypeString => OutPutRecipeType.ToString();
        public GuiRecipeCreator(ICoreClientAPI capi)
    : base(capi)
        {
            capi.ChatCommands.GetOrCreate("aca").WithDescription("AculinaryArtillery Commands").RequiresPlayer().RequiresPrivilege(Privilege.controlserver).BeginSubCommand("recipecreate")
            .WithRootAlias("recipecreate")
            .WithDescription("Opens the Recipe Creator")
            .HandleWith(CmdRecipeCreator)
            .EndSubCommand();

            /*
            capi.ChatCommands.GetOrCreate("dev").WithDescription("Gamedev tools").BeginSubCommand("recipeedit")
                .WithRootAlias("recipeedit")
                .WithDescription("Opens the Recipe Editor")
                .HandleWith(CmdRecipeEditor)
                .EndSubCommand();
            */

        }
        private TextCommandResult CmdRecipeCreator(TextCommandCallingArgs args)
        {
            //Slot 0-5 is Input
            //Slot 6 is Output
            Inventory = capi.World.Player.InventoryManager.GetInventory("inventoryRecipeCreator-"+capi.World.Player.PlayerUID) as InventoryRecipeCreator;
            if(Inventory != null)
            {
                capi.Network.SendPacketClient((Packet_Client)Inventory.Open(capi.World.Player));
                setUpDialog();
                TryOpen();
                return TextCommandResult.Success();
                //capi.World.Player.InventoryManager.Inventories.Add(Inventory.InventoryID, Inventory);
            }
            //Inventory.Open(capi.World.Player);
            return TextCommandResult.Error("Inventory was null");

            
        }
        private void setUpDialog()
        {
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            ElementBounds slotElementBounds = ElementBounds.Fixed(10, 50, 1, 1);
            slotElementBounds.BothSizing = ElementSizing.FitToChildren;

            ElementBounds outputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.RightBottom, 0, 10, 1, 1);
            ElementBounds ingredSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.LeftTop, 0, 0, 6, 1);

            ElementBounds outPutTextBounds = ElementBounds.Fixed(0, 50, 200, 50);


            ElementBounds copyRecipeBtnBounds = ElementStdBounds.ToggleButton(0, 0, 25, 25);
            copyRecipeBtnBounds.WithAlignment(EnumDialogArea.RightBottom);

            ElementBounds changeBtnBounds = ElementStdBounds.ToggleButton(0, 0, 25, 25);
            changeBtnBounds.WithAlignment(EnumDialogArea.LeftBottom);

            ElementBounds clearBtnBounds = ElementStdBounds.ToggleButton(0, 0, 25, 25);
            clearBtnBounds.WithAlignment(EnumDialogArea.CenterBottom);
            clearBtnBounds.WithFixedAlignmentOffset(30, 0);

            ElementBounds jsonOutPut = ElementBounds.FixedSize(400, 400);
            //jsonOutPut.BothSizing = ElementSizing.FitToChildren;
            ElementBounds textArea = ElementBounds.FixedSize(400.0, 400.0);
            ElementBounds scrollbarBounds = ElementStdBounds.VerticalScrollbar(jsonOutPut);
            scrollbarBounds.WithParent(jsonOutPut);
            ElementBounds guiBounds = ElementBounds.Fixed(0, 0, 400, 600);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds test = ElementBounds.FixedSize(100, 50);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(guiBounds);

            SingleComposer =
                capi.Gui.CreateCompo("Recipe Creator", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Recipe Creator", OnTitleBarClose)
                .BeginChildElements(bgBounds)
                    .BeginChildElements(slotElementBounds)
                        .AddItemSlotGrid(Inventory, SendInvPacket, 6, new int[] { 0, 1, 2, 3, 4, 5 }, ingredSlotBounds, "ingredSlots")
                        .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 6 }, outputSlotBounds, "outputSlot")
                        .AddDynamicText(OutPutRecipeTypeString, CairoFont.WhiteDetailText().Clone().WithFontSize(30), outPutTextBounds, "outputText")
                    .EndChildElements()
                    .AddStaticText("Json Code", CairoFont.WhiteDetailText(), test.FixedUnder(slotElementBounds, 100))
                    .BeginClip(jsonOutPut.FixedUnder(slotElementBounds, 125))
                    .AddTextArea(textArea, onTextChanged, CairoFont.WhiteSmallText(), "textareajson")
                    .EndClip()
                    .AddVerticalScrollbar(onNewScrollbarValue, scrollbarBounds, "scrollbar")
                    .AddSmallButton("Change Recipe Type", ChangeRecType, changeBtnBounds)
                    .AddSmallButton("Clear", Clear, clearBtnBounds)
                    .AddSmallButton("Copy Recipe", OnCopyRecipe, copyRecipeBtnBounds)
                .EndChildElements()
                .Compose();
            SingleComposer.GetScrollbar("scrollbar").SetHeights((float)20, (float)textArea.fixedHeight + 100);
        }
        private void onNewScrollbarValue(float value)
        {
            GuiElementTextArea textArea = SingleComposer.GetTextArea("textareajson");
            textArea.Bounds.fixedY = 1 - value;
            textArea.Bounds.CalcWorldBounds();
        }
        private void onTextChanged(string text)
        {
            outputJson = text;
        }
        private void SendInvPacket(object p)
        {
            if (Inventory[6].Empty)
            {
                SingleComposer.GetTextArea("textareajson").SetValue("");
            }
            else
            {
                getJson();
                SingleComposer.GetTextArea("textareajson").SetValue(outputJson);
            }

            capi.Network.SendPacketClient(p);
        }
        private void OnTitleBarClose()
        {
            TryClose();
        }

        private bool Clear()
        {
            Inventory.DiscardAll();
            capi.Network.GetChannel("clearRecipeCreatorInv").SendPacket(true);
            outputJson = "";
            SingleComposer.GetTextArea("textareajson").SetValue("");

            return true;
        }
        private bool ChangeRecType()
        {
            if(OutPutRecipeType == recType.Kneading) { OutPutRecipeType = recType.Simmering; } else { OutPutRecipeType = recType.Kneading; }
            SingleComposer.GetDynamicText("outputText").SetNewText(OutPutRecipeTypeString);
            if (!Inventory[6].Empty) 
            {
                getJson();
                SingleComposer.GetTextArea("textareajson").SetValue(outputJson);
            }
            
            return true;
        }
        private bool OnCopyRecipe()
        {
            if (Inventory[6].Empty)
            {
                capi.ShowChatMessage("Need an item in the output slot");
                SingleComposer.GetTextArea("textareajson").SetValue("");
                return false;
            }
            ScreenManager.Platform.XPlatInterface.SetClipboardText(outputJson);
            return true;
        }
        private void getJson()
        {
            object recipe = null;
            if(OutPutRecipeType == recType.Kneading)
            {
                List<ingredientStub> tmpIngredients = new List<ingredientStub>();
                for (int i = 0; i < 6; i++)
                {
                    if (Inventory[i].Itemstack == null) continue;
                    List<inputStub> tmpinputs = new List<inputStub>
                {
                    new inputStub { type = Inventory[i].Itemstack.Collectible.ItemClass.Name(), code = Inventory[i].Itemstack.Collectible.Code.ToString(), quantity = Inventory[i].StackSize }
                };
                    tmpIngredients.Add(new ingredientStub { inputs = tmpinputs });
                }
                outputStub tmpoutput = new outputStub { type = Inventory[6].Itemstack.Collectible.ItemClass.Name(), code = Inventory[6].Itemstack.Collectible.Code.ToString(), stackSize = Inventory[6].StackSize };
                recipe = new recipeStubKneading { enabled = true, ingredients = tmpIngredients, output = tmpoutput };
            }
            else if(OutPutRecipeType == recType.Simmering)
            {
                List<inputStub> tmpIngredients = new List<inputStub>();
                for (int i = 0; i < 6; i++)
                {
                    if (Inventory[i].Itemstack == null) continue;
                    tmpIngredients.Add(new inputStub { type = Inventory[i].Itemstack.Collectible.ItemClass.Name(), code = Inventory[i].Itemstack.Collectible.Code.ToString(), quantity = Inventory[i].StackSize });
                }
                smeltedStackStub tmpoutput = new smeltedStackStub {type = Inventory[6].Itemstack.Collectible.ItemClass.Name(), code = Inventory[6].Itemstack.Collectible.Code.ToString()};
                simmeringPropsStub tmpSimmerProps = new simmeringPropsStub { meltingPoint = 200, meltingDuration = 20, smeltedRatio = 1, smeltingType = "bake", smeltedStack = tmpoutput, requiresContainer = false};
                recipe = new recipeStubSimmering { enabled = true, ingredients = tmpIngredients,  Simmering = tmpSimmerProps };
            }
            outputJson = JsonConvert.SerializeObject(recipe, Formatting.Indented);
        }
        public override string ToggleKeyCombinationCode => null;
    }
    public class smeltedStackStub
    {
        public string type;
        public string code;
    }
    public class simmeringPropsStub
    {
        public int meltingPoint;
        public int meltingDuration;
        public float smeltedRatio;
        public string smeltingType;
        public smeltedStackStub smeltedStack;
        public bool requiresContainer;
    }
    public class recipeStubKneading
    {
        public bool enabled = true;
        public List<ingredientStub> ingredients;
        public outputStub output;
    }
    public class recipeStubSimmering
    {
        public List<inputStub> ingredients;
        public bool enabled = true;
        public simmeringPropsStub Simmering;
    }
    public class outputStub
    {
        public string type;
        public string code;
        public int stackSize;
    }
    public class inputStub
    {
        public string type;
        public string code;
        public int quantity;
    }
    public class ingredientStub
    {
        public List<inputStub> inputs;
    }
}
